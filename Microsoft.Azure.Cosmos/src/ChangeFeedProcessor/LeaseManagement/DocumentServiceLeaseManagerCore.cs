﻿namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Exceptions;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Utils;

    internal sealed class DocumentServiceLeaseManagerCore : DocumentServiceLeaseManager
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly CosmosContainer leaseContainer;
        private readonly DocumentServiceLeaseUpdater leaseUpdater;
        private readonly DocumentServiceLeaseStoreManagerSettings settings;
        private readonly RequestOptionsFactory requestOptionsFactory;

        public DocumentServiceLeaseManagerCore(
            CosmosContainer leaseContainer,
            DocumentServiceLeaseUpdater leaseUpdater,
            DocumentServiceLeaseStoreManagerSettings settings,
            RequestOptionsFactory requestOptionsFactory)
        {
            this.leaseContainer = leaseContainer;
            this.leaseUpdater = leaseUpdater;
            this.settings = settings;
            this.requestOptionsFactory = requestOptionsFactory;
        }

        public override async Task<DocumentServiceLease> AcquireAsync(DocumentServiceLease lease)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            string oldOwner = lease.Owner;

            return await this.leaseUpdater.UpdateLeaseAsync(
                lease,
                lease.Id,
                this.requestOptionsFactory.GetPartitionKey(lease.Id),
                serverLease =>
                {
                    if (serverLease.Owner != oldOwner)
                    {
                        Logger.InfoFormat("Partition {0} lease was taken over by owner '{1}'", lease.PartitionId, serverLease.Owner);
                        throw new LeaseLostException(lease);
                    }
                    serverLease.Owner = this.settings.HostName;
                    serverLease.Properties = lease.Properties;
                    return serverLease;
                }).ConfigureAwait(false);
        }

        public override async Task<DocumentServiceLease> CreateLeaseIfNotExistAsync(string partitionId, string continuationToken)
        {
            if (partitionId == null)
                throw new ArgumentNullException(nameof(partitionId));

            string leaseDocId = this.GetDocumentId(partitionId);
            var documentServiceLease = new DocumentServiceLeaseCore
            {
                LeaseId = leaseDocId,
                DistributionUnit = partitionId,
                ContinuationToken = continuationToken,
            };

            bool created = await this.leaseContainer.TryCreateItemAsync<DocumentServiceLeaseCore>(
                this.requestOptionsFactory.GetPartitionKey(documentServiceLease.Id),
                documentServiceLease).ConfigureAwait(false) != null;
            if (created)
            {
                Logger.InfoFormat("Created lease for partition {0}.", partitionId);
                return documentServiceLease;
            }

            Logger.InfoFormat("Some other host created lease for {0}.", partitionId);
            return null;
        }

        public override async Task ReleaseAsync(DocumentServiceLease lease)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            DocumentServiceLeaseCore refreshedLease = await this.TryGetLeaseAsync(lease).ConfigureAwait(false);
            if (refreshedLease == null)
            {
                Logger.InfoFormat("Partition {0} failed to release lease. The lease is gone already.", lease.PartitionId);
                throw new LeaseLostException(lease);
            }

            await this.leaseUpdater.UpdateLeaseAsync(
                refreshedLease,
                refreshedLease.Id,
                this.requestOptionsFactory.GetPartitionKey(lease.Id),
                serverLease =>
                {
                    if (serverLease.Owner != lease.Owner)
                    {
                        Logger.InfoFormat("Partition {0} no need to release lease. The lease was already taken by another host '{1}'.", lease.PartitionId, serverLease.Owner);
                        throw new LeaseLostException(lease);
                    }
                    serverLease.Owner = null;
                    return serverLease;
                }).ConfigureAwait(false);
        }

        public override async Task DeleteAsync(DocumentServiceLease lease)
        {
            if (lease?.Id == null)
            {
                throw new ArgumentNullException(nameof(lease));
            }

            await this.leaseContainer.TryDeleteItemAsync<DocumentServiceLeaseCore>(
                    this.requestOptionsFactory.GetPartitionKey(lease.Id),
                    lease.Id).ConfigureAwait(false);
        }

        public override async Task<DocumentServiceLease> RenewAsync(DocumentServiceLease lease)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            // Get fresh lease. The assumption here is that checkpointing is done with higher frequency than lease renewal so almost
            // certainly the lease was updated in between.
            DocumentServiceLeaseCore refreshedLease = await this.TryGetLeaseAsync(lease).ConfigureAwait(false);
            if (refreshedLease == null)
            {
                Logger.InfoFormat("Partition {0} failed to renew lease. The lease is gone already.", lease.PartitionId);
                throw new LeaseLostException(lease);
            }

            return await this.leaseUpdater.UpdateLeaseAsync(
                refreshedLease,
                refreshedLease.Id,
                this.requestOptionsFactory.GetPartitionKey(lease.Id),
                serverLease =>
                {
                    if (serverLease.Owner != lease.Owner)
                    {
                        Logger.InfoFormat("Partition {0} lease was taken over by owner '{1}'", lease.PartitionId, serverLease.Owner);
                        throw new LeaseLostException(lease);
                    }
                    return serverLease;
                }).ConfigureAwait(false);
        }

        public override async Task<DocumentServiceLease> UpdatePropertiesAsync(DocumentServiceLease lease)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));

            if (lease.Owner != this.settings.HostName)
            {
                Logger.InfoFormat("Partition '{0}' lease was taken over by owner '{1}' before lease properties update", lease.PartitionId, lease.Owner);
                throw new LeaseLostException(lease);
            }

            return await this.leaseUpdater.UpdateLeaseAsync(
                lease,
                lease.Id,
                this.requestOptionsFactory.GetPartitionKey(lease.Id),
                serverLease =>
                {
                    if (serverLease.Owner != lease.Owner)
                    {
                        Logger.InfoFormat("Partition '{0}' lease was taken over by owner '{1}'", lease.PartitionId, serverLease.Owner);
                        throw new LeaseLostException(lease);
                    }
                    serverLease.Properties = lease.Properties;
                    return serverLease;
                }).ConfigureAwait(false);
        }

        private async Task<DocumentServiceLeaseCore> TryGetLeaseAsync(DocumentServiceLease lease)
        {
            return await this.leaseContainer.TryGetItemAsync<DocumentServiceLeaseCore>(this.requestOptionsFactory.GetPartitionKey(lease.Id), lease.Id).ConfigureAwait(false);
        }

        private string GetDocumentId(string partitionId)
        {
            return this.settings.GetPartitionLeasePrefix() + partitionId;
        }
    }
}
