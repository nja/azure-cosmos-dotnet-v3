// ----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//  ----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    using System;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;

    internal class PartitionProcessorFactory : IPartitionProcessorFactory
    {
        private readonly CosmosContainer container;
        private readonly ChangeFeedProcessorOptions changeFeedProcessorOptions;
        private readonly ILeaseCheckpointer leaseCheckpointer;

        public PartitionProcessorFactory(
            CosmosContainer container,
            ChangeFeedProcessorOptions changeFeedProcessorOptions,
            ILeaseCheckpointer leaseCheckpointer)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (changeFeedProcessorOptions == null) throw new ArgumentNullException(nameof(changeFeedProcessorOptions));
            if (leaseCheckpointer == null) throw new ArgumentNullException(nameof(leaseCheckpointer));

            this.container = container;
            this.changeFeedProcessorOptions = changeFeedProcessorOptions;
            this.leaseCheckpointer = leaseCheckpointer;
        }

        public IPartitionProcessor Create(ILease lease, IChangeFeedObserver observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            if (lease == null) throw new ArgumentNullException(nameof(lease));

            var settings = new ProcessorSettings
            {
                StartContinuation = !string.IsNullOrEmpty(lease.ContinuationToken) ?
                    lease.ContinuationToken :
                    this.changeFeedProcessorOptions.StartContinuation,
                PartitionKeyRangeId = lease.PartitionId,
                FeedPollDelay = this.changeFeedProcessorOptions.FeedPollDelay,
                MaxItemCount = this.changeFeedProcessorOptions.MaxItemCount,
                StartFromBeginning = this.changeFeedProcessorOptions.StartFromBeginning,
                StartTime = this.changeFeedProcessorOptions.StartTime,
                SessionToken = this.changeFeedProcessorOptions.SessionToken,
            };

            var checkpointer = new PartitionCheckpointer(this.leaseCheckpointer, lease);
            return new PartitionProcessor(observer, this.container, settings, checkpointer);
        }
    }
}
