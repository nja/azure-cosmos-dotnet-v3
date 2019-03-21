//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Internal;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Exceptions;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;
    

    internal class DocumentServiceLeaseUpdater : IDocumentServiceLeaseUpdater
    {
        private const int RetryCountOnConflict = 5;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly CosmosContainer container;

        public DocumentServiceLeaseUpdater(CosmosContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            this.container = container;
        }

        public async Task<ILease> UpdateLeaseAsync(ILease cachedLease, string itemId, object partitionKey, Func<ILease, ILease> updateLease)
        {
            ILease lease = cachedLease;
            for (int retryCount = RetryCountOnConflict; retryCount >= 0; retryCount--)
            {
                lease = updateLease(lease);
                if (lease == null)
                {
                    return null;
                }

                lease.Timestamp = DateTime.UtcNow;
                DocumentServiceLease leaseDocument = await this.TryReplaceLeaseAsync((DocumentServiceLease)lease, partitionKey, itemId).ConfigureAwait(false);

                Logger.InfoFormat("Partition {0} lease update conflict. Reading the current version of lease.", lease.PartitionId);
                DocumentServiceLease serverLease;
                try
                {
                    var response = await this.container.Items.ReadItemAsync<DocumentServiceLease>(
                        partitionKey, itemId).ConfigureAwait(false);
                    serverLease = response.Resource;
                }
                catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Logger.InfoFormat("Partition {0} lease no longer exists", lease.PartitionId);
                    throw new LeaseLostException(lease);
                }

                Logger.InfoFormat(
                    "Partition {0} update failed because the lease with token '{1}' was updated by host '{2}' with token '{3}'. Will retry, {4} retry(s) left.",
                    lease.PartitionId,
                    lease.ConcurrencyToken,
                    serverLease.Owner,
                    serverLease.ConcurrencyToken,
                    retryCount);

                lease = serverLease;
            }

            throw new LeaseLostException(lease);
        }

        private async Task<DocumentServiceLease> TryReplaceLeaseAsync(DocumentServiceLease lease, object partitionKey, string itemId)
        {
            try
            {
                var response = await this.container.Items.ReplaceItemAsync<DocumentServiceLease>(
                    partitionKey,
                    itemId, 
                    lease, 
                    this.CreateIfMatchOptions(lease)).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    return null;
                }

                if (response.StatusCode == HttpStatusCode.Conflict ||
                    response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new LeaseLostException(lease, response.StatusCode == HttpStatusCode.NotFound);
                }

                return response.Resource;
            }
            catch (CosmosException ex)
            {
                Logger.WarnFormat("Lease operation exception, status code: ", ex.StatusCode);
                if (ex.StatusCode == HttpStatusCode.Conflict ||
                    ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new LeaseLostException(lease, ex, ex.StatusCode == HttpStatusCode.NotFound);
                }

                throw;
            }
        }

        private CosmosItemRequestOptions CreateIfMatchOptions(ILease lease)
        {
            var ifMatchCondition = new AccessCondition { Type = AccessConditionType.IfMatch, Condition = lease.ConcurrencyToken };
            return new CosmosItemRequestOptions { AccessCondition = ifMatchCondition };
        }
    }
}
