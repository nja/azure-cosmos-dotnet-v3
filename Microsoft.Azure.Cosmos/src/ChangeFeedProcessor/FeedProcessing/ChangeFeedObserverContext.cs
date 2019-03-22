//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.FeedProcessing
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    /// <summary>
    /// Represents the context passed to <see cref="ChangeFeedObserver"/> events.
    /// </summary>
    public abstract class ChangeFeedObserverContext
    {
        /// <summary>
        /// Gets the id of the partition for the current event.
        /// </summary>
        public abstract string PartitionKeyRangeId { get; }

        //public abstract IFeedResponse<Document> FeedResponse { get; }

        /// <summary>
        /// Checkpoints progress of a stream. This method is valid only if manual checkpoint was configured.
        /// Client may accept multiple change feed batches to process in parallel.
        /// Once first N document processing was finished the client can call checkpoint on the last completed batches in the row.
        /// In case of automatic checkpointing this is method throws.
        /// </summary>
        /// <exception cref="Exceptions.LeaseLostException">Thrown if other host acquired the lease or the lease was deleted</exception>
        public abstract Task CheckpointAsync();
    }
}