//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;
    using Microsoft.Azure.Cosmos;

    /// <summary>
    /// Used to create request options for non-partitioned lease collections.
    /// </summary>
    internal class SinglePartitionRequestOptionsFactory : IRequestOptionsFactory
    {
        public FeedOptions CreateFeedOptions() => null;

        public string GetPartitionKey(string itemId) => null;
    }
}
