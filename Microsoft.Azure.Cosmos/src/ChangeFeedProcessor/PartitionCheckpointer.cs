//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Logging;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;

    internal class PartitionCheckpointer : IPartitionCheckpointer
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly ILeaseCheckpointer leaseCheckpointer;
        private ILease lease;

        public PartitionCheckpointer(ILeaseCheckpointer leaseCheckpointer, ILease lease)
        {
            this.leaseCheckpointer = leaseCheckpointer;
            this.lease = lease;
        }

        public async Task CheckpointPartitionAsync(string сontinuationToken)
        {
            this.lease = await this.leaseCheckpointer.CheckpointAsync(this.lease, сontinuationToken).ConfigureAwait(false);
            Logger.InfoFormat("Checkpoint: partition {0}, new continuation {1}", this.lease.PartitionId, this.lease.ContinuationToken);
        }
    }
}