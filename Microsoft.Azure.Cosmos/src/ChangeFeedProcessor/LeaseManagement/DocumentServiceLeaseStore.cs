//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.ChangeFeedProcessor.LeaseManagement
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.ChangeFeedProcessor.Utils;

    internal class DocumentServiceLeaseStore : ILeaseStore
    {
        private readonly CosmosContainer container;
        private readonly string containerNamePrefix;
        private readonly IRequestOptionsFactory requestOptionsFactory;
        private string lockETag;

        public DocumentServiceLeaseStore(
            CosmosContainer container,
            string containerNamePrefix,
            IRequestOptionsFactory requestOptionsFactory)
        {
            this.container = container;
            this.containerNamePrefix = containerNamePrefix;
            this.requestOptionsFactory = requestOptionsFactory;
        }

        public async Task<bool> IsInitializedAsync()
        {
            string markerDocId = this.GetStoreMarkerName();

            return await this.container.ItemExistsAsync(this.requestOptionsFactory.GetPartitionKey(markerDocId), markerDocId).ConfigureAwait(false);
        }

        public async Task MarkInitializedAsync()
        {
            string markerDocId = this.GetStoreMarkerName();
            var containerDocument = new { id = markerDocId };
            await this.container.Items.CreateItemAsync<dynamic>(
                this.requestOptionsFactory.GetPartitionKey(markerDocId),
                containerDocument
                ).ConfigureAwait(false);
        }

        public async Task<bool> AcquireInitializationLockAsync(TimeSpan lockTime)
        {
            string lockId = this.GetStoreLockName();
            var containerDocument = new { id = lockId, ttl = (int)lockTime.TotalSeconds };
            var document = await this.container.TryCreateItemAsync<dynamic>(
                this.requestOptionsFactory.GetPartitionKey(lockId),
                containerDocument).ConfigureAwait(false);

            if (document != null)
            {
                this.lockETag = document.etag;
                return true;
            }

            return false;
        }

        public async Task<bool> ReleaseInitializationLockAsync()
        {
            string lockId = this.GetStoreLockName();
            var requestOptions = new CosmosItemRequestOptions()
            {
                AccessCondition = new AccessCondition { Type = AccessConditionType.IfMatch, Condition = this.lockETag }
            };

            var document = await this.container.TryDeleteItemAsync<dynamic>(
                this.requestOptionsFactory.GetPartitionKey(lockId),
                lockId,
                requestOptions).ConfigureAwait(false);

            if (document != null)
            {
                this.lockETag = null;
                return true;
            }

            return false;
        }

        private string GetStoreMarkerName()
        {
            return this.containerNamePrefix + ".info";
        }

        private string GetStoreLockName()
        {
            return this.containerNamePrefix + ".lock";
        }
    }
}