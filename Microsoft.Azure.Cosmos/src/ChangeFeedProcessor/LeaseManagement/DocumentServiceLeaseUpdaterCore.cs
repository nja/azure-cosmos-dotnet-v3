﻿//----------------------------------------------------------------
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


    internal sealed class DocumentServiceLeaseUpdaterCore : DocumentServiceLeaseUpdater
    {
        private const int RetryCountOnConflict = 5;
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly CosmosContainer container;

        public DocumentServiceLeaseUpdaterCore(CosmosContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            this.container = container;
        }

        public override async Task<DocumentServiceLease> UpdateLeaseAsync(DocumentServiceLease cachedLease, string itemId, object partitionKey, Func<DocumentServiceLease, DocumentServiceLease> updateLease)
        {
            DocumentServiceLease lease = cachedLease;
            for (int retryCount = RetryCountOnConflict; retryCount >= 0; retryCount--)
            {
                lease = updateLease(lease);
                if (lease == null)
                {
                    return null;
                }

                lease.Timestamp = DateTime.UtcNow;
                DocumentServiceLeaseCore leaseDocument = await this.TryReplaceLeaseAsync((DocumentServiceLeaseCore)lease, partitionKey, itemId).ConfigureAwait(false);
                if (leaseDocument != null)
                {
                    return leaseDocument;
                }

                Logger.InfoFormat("Partition {0} lease update conflict. Reading the current version of lease.", lease.PartitionId);
                DocumentServiceLeaseCore serverLease;
                try
                {
                    var response = await this.container.Items.ReadItemAsync<DocumentServiceLeaseCore>(
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

        private async Task<DocumentServiceLeaseCore> TryReplaceLeaseAsync(DocumentServiceLeaseCore lease, object partitionKey, string itemId)
        {
            try
            {
                var response = await this.container.Items.ReplaceItemAsync<DocumentServiceLeaseCore>(
                    partitionKey,
                    itemId, 
                    lease, 
                    this.CreateIfMatchOptions(lease)).ConfigureAwait(false);

                return response.Resource;
            }
            catch (CosmosException ex)
            {
                Logger.WarnFormat("Lease operation exception, status code: ", ex.StatusCode);
                if (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    return null;
                }

                if (ex.StatusCode == HttpStatusCode.Conflict ||
                    ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new LeaseLostException(lease, ex, ex.StatusCode == HttpStatusCode.NotFound);
                }

                throw;
            }
        }

        private CosmosItemRequestOptions CreateIfMatchOptions(DocumentServiceLease lease)
        {
            var ifMatchCondition = new AccessCondition { Type = AccessConditionType.IfMatch, Condition = lease.ConcurrencyToken };
            return new CosmosItemRequestOptions { AccessCondition = ifMatchCondition };
        }
    }
}
