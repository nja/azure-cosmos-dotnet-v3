﻿// ----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//  ----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    using System;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;

    internal class PartitionProcessorFactoryCore<T> : PartitionProcessorFactory<T>
    {
        private readonly CosmosContainer container;
        private readonly ChangeFeedProcessorOptions changeFeedProcessorOptions;
        private readonly DocumentServiceLeaseCheckpointer leaseCheckpointer;

        public PartitionProcessorFactoryCore(
            CosmosContainer container,
            ChangeFeedProcessorOptions changeFeedProcessorOptions,
            DocumentServiceLeaseCheckpointer leaseCheckpointer)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (changeFeedProcessorOptions == null) throw new ArgumentNullException(nameof(changeFeedProcessorOptions));
            if (leaseCheckpointer == null) throw new ArgumentNullException(nameof(leaseCheckpointer));

            this.container = container;
            this.changeFeedProcessorOptions = changeFeedProcessorOptions;
            this.leaseCheckpointer = leaseCheckpointer;
        }

        public override PartitionProcessor Create(DocumentServiceLease lease, ChangeFeedObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (lease == null) throw new ArgumentNullException(nameof(lease));

            var settings = new ProcessorSettings
            {
                StartContinuation = !string.IsNullOrEmpty(lease.ContinuationToken) ?
                    lease.ContinuationToken :
                    this.changeFeedProcessorOptions.StartContinuation,
                PartitionKeyRangeId = lease.CurrentLeaseToken,
                FeedPollDelay = this.changeFeedProcessorOptions.FeedPollDelay,
                MaxItemCount = this.changeFeedProcessorOptions.MaxItemCount,
                StartFromBeginning = this.changeFeedProcessorOptions.StartFromBeginning,
                StartTime = this.changeFeedProcessorOptions.StartTime,
                SessionToken = this.changeFeedProcessorOptions.SessionToken,
            };

            var checkpointer = new PartitionCheckpointerCore(this.leaseCheckpointer, lease);
            return new PartitionProcessorCore<T>(observer, this.container, settings, checkpointer);
        }
    }
}
