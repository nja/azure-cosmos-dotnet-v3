//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.PartitionManagement;
    using Microsoft.Azure.Cosmos;

    /// <summary>
    /// Defines request options for lease requests to use with <see cref="DocumentServiceLeaseStoreManager"/> and <see cref="DocumentServiceLeaseStore"/>.
    /// </summary>
    internal interface IRequestOptionsFactory
    {
        string GetPartitionKey(string itemId);

        FeedOptions CreateFeedOptions();
    }
}
