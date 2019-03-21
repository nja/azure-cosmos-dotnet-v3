//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Exceptions;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Utils;
    using Microsoft.Azure.Cosmos.Internal;
    using Microsoft.Azure.Cosmos.Linq;

    /// <summary>
    /// Lease manager that is using Azure Document Service as lease storage.
    /// Documents in lease collection are organized as this:
    /// ChangeFeed.federation|database_rid|collection_rid.info            -- container
    /// ChangeFeed.federation|database_rid|collection_rid..partitionId1   -- each partition
    /// ChangeFeed.federation|database_rid|collection_rid..partitionId2
    ///                                         ...
    /// </summary>
    internal class DocumentServiceLeaseStoreManager : ILeaseStoreManager
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly DocumentServiceLeaseStoreManagerSettings settings;
        private readonly CosmosContainer leaseContainer;
        private readonly IRequestOptionsFactory requestOptionsFactory;
        private readonly IDocumentServiceLeaseUpdater leaseUpdater;
        private readonly ILeaseStore leaseStore;

        public DocumentServiceLeaseStoreManager(
            DocumentServiceLeaseStoreManagerSettings settings,
            CosmosContainer leaseContainer,
            IRequestOptionsFactory requestOptionsFactory)
            : this(settings, leaseContainer, requestOptionsFactory, new DocumentServiceLeaseUpdater(leaseContainer))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentServiceLeaseStoreManager"/> class.
        /// </summary>
        /// <remarks>
        /// Internal only for testing purposes, otherwise would be private.
        /// </remarks>
        internal DocumentServiceLeaseStoreManager(
            DocumentServiceLeaseStoreManagerSettings settings,
            CosmosContainer leaseContainer,
            IRequestOptionsFactory requestOptionsFactory,
            IDocumentServiceLeaseUpdater leaseUpdater) // For testing purposes only.
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.ContainerNamePrefix == null) throw new ArgumentNullException(nameof(settings.ContainerNamePrefix));
            if (string.IsNullOrEmpty(settings.HostName)) throw new ArgumentNullException(nameof(settings.HostName));
            if (leaseContainer == null) throw new ArgumentNullException(nameof(leaseContainer));
            if (requestOptionsFactory == null) throw new ArgumentException(nameof(requestOptionsFactory));
            if (leaseUpdater == null) throw new ArgumentException(nameof(leaseUpdater));

            this.settings = settings;
            this.leaseContainer = leaseContainer;
            this.requestOptionsFactory = requestOptionsFactory;
            this.leaseUpdater = leaseUpdater;
            this.leaseStore = new DocumentServiceLeaseStore(
                this.leaseContainer,
                this.settings.ContainerNamePrefix,
                this.requestOptionsFactory);
        }

        public async Task<IReadOnlyList<ILease>> GetAllLeasesAsync()
        {
            return await this.ListDocumentsAsync(this.GetPartitionLeasePrefix()).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ILease>> GetOwnedLeasesAsync()
        {
            var ownedLeases = new List<ILease>();
            foreach (ILease lease in await this.GetAllLeasesAsync().ConfigureAwait(false))
            {
                if (string.Compare(lease.Owner, this.settings.HostName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ownedLeases.Add(lease);
                }
            }

            return ownedLeases;
        }

        public async Task<ILease> CreateLeaseIfNotExistAsync(string partitionId, string continuationToken)
        {
            if (partitionId == null)
                throw new ArgumentNullException(nameof(partitionId));

            string leaseDocId = this.GetDocumentId(partitionId);
            var documentServiceLease = new DocumentServiceLease
            {
                Id = leaseDocId,
                PartitionId = partitionId,
                ContinuationToken = continuationToken,
            };

            bool created = await this.leaseContainer.TryCreateItemAsync<DocumentServiceLease>(
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

        public async Task<ILease> CheckpointAsync(ILease lease, string continuationToken)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            if (string.IsNullOrEmpty(continuationToken))
                throw new ArgumentException("continuationToken must be a non-empty string", nameof(continuationToken));

            return await this.leaseUpdater.UpdateLeaseAsync(
                lease,
                lease.Id,
                this.requestOptionsFactory.GetPartitionKey(lease.Id),
                serverLease =>
                {
                    if (serverLease.Owner != lease.Owner)
                    {
                        Logger.InfoFormat("Partition {0} lease was taken over by owner '{1}'", lease.PartitionId, serverLease.Owner);
                        throw new LeaseLostException(lease);
                    }
                    serverLease.ContinuationToken = continuationToken;
                    return serverLease;
                }).ConfigureAwait(false);
        }

        public async Task<ILease> AcquireAsync(ILease lease)
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

        public async Task<ILease> RenewAsync(ILease lease)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            // Get fresh lease. The assumption here is that checkpointing is done with higher frequency than lease renewal so almost
            // certainly the lease was updated in between.
            DocumentServiceLease refreshedLease = await this.TryGetLeaseAsync(lease).ConfigureAwait(false);
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

        public async Task ReleaseAsync(ILease lease)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            DocumentServiceLease refreshedLease = await this.TryGetLeaseAsync(lease).ConfigureAwait(false);
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

        public async Task DeleteAsync(ILease lease)
        {
            if (lease?.Id == null)
            {
                throw new ArgumentNullException(nameof(lease));
            }

            await this.leaseContainer.TryDeleteItemAsync<DocumentServiceLease>(
                    this.requestOptionsFactory.GetPartitionKey(lease.Id),
                    lease.Id).ConfigureAwait(false);
        }

        public async Task<ILease> UpdatePropertiesAsync(ILease lease)
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

        public Task<bool> IsInitializedAsync()
        {
            return this.leaseStore.IsInitializedAsync();
        }

        public Task MarkInitializedAsync()
        {
            return this.leaseStore.MarkInitializedAsync();
        }

        public Task<bool> AcquireInitializationLockAsync(TimeSpan lockExpirationTime)
        {
            return this.leaseStore.AcquireInitializationLockAsync(lockExpirationTime);
        }

        public Task<bool> ReleaseInitializationLockAsync()
        {
            return this.leaseStore.ReleaseInitializationLockAsync();
        }

        private async Task<DocumentServiceLease> TryGetLeaseAsync(ILease lease)
        {
            return await this.leaseContainer.TryGetItemAsync<DocumentServiceLease>(this.requestOptionsFactory.GetPartitionKey(lease.Id), lease.Id).ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<DocumentServiceLease>> ListDocumentsAsync(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("Prefix must be non-empty string", nameof(prefix));

            var query = this.leaseContainer.Items.CreateItemQuery<DocumentServiceLease>(
                "SELECT * FROM c WHERE STARTSWITH(c.id, '" + prefix + "')",
                0 /* max concurrency */);
            var leases = new List<DocumentServiceLease>();
            while (query.HasMoreResults)
            {
                leases.AddRange(await query.FetchNextSetAsync().ConfigureAwait(false));
            }

            return leases;
        }

        private string GetDocumentId(string partitionId)
        {
            return this.GetPartitionLeasePrefix() + partitionId;
        }

        private string GetPartitionLeasePrefix()
        {
            return this.settings.ContainerNamePrefix + "..";
        }
    }
}