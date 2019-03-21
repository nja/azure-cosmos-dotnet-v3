//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;
    using Microsoft.Azure.Cosmos;

    /// <summary>
    /// Used to create request options for partitioned lease collections, when partition key is defined as /id.
    /// </summary>
    internal class PartitionedByIdCollectionRequestOptionsFactory : IRequestOptionsFactory
    {
        public string GetPartitionKey(string itemId) => itemId;

        public FeedOptions CreateFeedOptions() => new FeedOptions { EnableCrossPartitionQuery = true };
    }
}
