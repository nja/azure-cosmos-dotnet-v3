//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;

    /// <summary>
    /// Lease manager that is using Azure Document Service as lease storage.
    /// Documents in lease collection are organized as this:
    /// ChangeFeed.federation|database_rid|collection_rid.info            -- container
    /// ChangeFeed.federation|database_rid|collection_rid..partitionId1   -- each partition
    /// ChangeFeed.federation|database_rid|collection_rid..partitionId2
    ///                                         ...
    /// </summary>
    internal sealed class DocumentServiceLeaseStoreManagerCore : DocumentServiceLeaseStoreManager
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly DocumentServiceLeaseStore leaseStore;
        private readonly DocumentServiceLeaseManager leaseManager;
        private readonly DocumentServiceLeaseCheckpointer leaseCheckpointer;
        private readonly DocumentServiceLeaseContainer leaseContainer;

        public DocumentServiceLeaseStoreManagerCore(
            DocumentServiceLeaseStoreManagerSettings settings,
            CosmosContainer leaseContainer,
            RequestOptionsFactory requestOptionsFactory)
            : this(settings, leaseContainer, requestOptionsFactory, new DocumentServiceLeaseUpdaterCore(leaseContainer))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentServiceLeaseStoreManagerCore"/> class.
        /// </summary>
        /// <remarks>
        /// Internal only for testing purposes, otherwise would be private.
        /// </remarks>
        internal DocumentServiceLeaseStoreManagerCore(
            DocumentServiceLeaseStoreManagerSettings settings,
            CosmosContainer container,
            RequestOptionsFactory requestOptionsFactory,
            DocumentServiceLeaseUpdater leaseUpdater) // For testing purposes only.
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.ContainerNamePrefix == null) throw new ArgumentNullException(nameof(settings.ContainerNamePrefix));
            if (string.IsNullOrEmpty(settings.HostName)) throw new ArgumentNullException(nameof(settings.HostName));
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (requestOptionsFactory == null) throw new ArgumentException(nameof(requestOptionsFactory));
            if (leaseUpdater == null) throw new ArgumentException(nameof(leaseUpdater));

            this.leaseStore = new DocumentServiceLeaseStoreCore(
                container,
                settings.ContainerNamePrefix,
                requestOptionsFactory);

            this.leaseManager = new DocumentServiceLeaseManagerCore(
                container,
                leaseUpdater,
                settings,
                requestOptionsFactory);

            this.leaseCheckpointer = new DocumentServiceLeaseCheckpointerCore(
                leaseUpdater,
                settings,
                requestOptionsFactory);

            this.leaseContainer = new DocumentServiceLeaseContainerCore(
                container,
                settings);
        }

        public override DocumentServiceLeaseStore LeaseStore => this.leaseStore;

        public override DocumentServiceLeaseManager LeaseManager => this.leaseManager;

        public override DocumentServiceLeaseCheckpointer LeaseCheckpointer => this.leaseCheckpointer;

        public override DocumentServiceLeaseContainer LeaseContainer => this.leaseContainer;
    }
}