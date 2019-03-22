﻿//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement
{
    using System;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement;

    internal sealed class PartitionSupervisorFactoryCore : PartitionSupervisorFactory
    {
        private readonly ChangeFeedObserverFactory observerFactory;
        private readonly DocumentServiceLeaseManager leaseManager;
        private readonly ChangeFeedProcessorOptions changeFeedProcessorOptions;
        private readonly PartitionProcessorFactory partitionProcessorFactory;

        public PartitionSupervisorFactoryCore(
            ChangeFeedObserverFactory observerFactory,
            DocumentServiceLeaseManager leaseManager,
            PartitionProcessorFactory partitionProcessorFactory,
            ChangeFeedProcessorOptions options)
        {
            if (observerFactory == null) throw new ArgumentNullException(nameof(observerFactory));
            if (leaseManager == null) throw new ArgumentNullException(nameof(leaseManager));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (partitionProcessorFactory == null) throw new ArgumentNullException(nameof(partitionProcessorFactory));

            this.observerFactory = observerFactory;
            this.leaseManager = leaseManager;
            this.changeFeedProcessorOptions = options;
            this.partitionProcessorFactory = partitionProcessorFactory;
        }

        public override PartitionSupervisor Create(DocumentServiceLease lease)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));

            ChangeFeedObserver changeFeedObserver = this.observerFactory.CreateObserver();
            var processor = this.partitionProcessorFactory.Create(lease, changeFeedObserver);
            var renewer = new LeaseRenewerCore(lease, this.leaseManager, this.changeFeedProcessorOptions.LeaseRenewInterval);

            return new PartitionSupervisorCore(lease, changeFeedObserver, processor, renewer);
        }
    }
}