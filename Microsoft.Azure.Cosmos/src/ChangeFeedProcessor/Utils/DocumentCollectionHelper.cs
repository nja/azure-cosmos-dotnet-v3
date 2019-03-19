//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Documents.ChangeFeedProcessor.Utils
{
    using Microsoft.Azure.Cosmos;

    internal static class DocumentCollectionHelper
    {
        private const string DefaultUserAgentSuffix = "changefeed-2.2.6";

        public static DocumentCollectionInfo Canonicalize(this DocumentCollectionInfo collectionInfo)
        {
            DocumentCollectionInfo result = collectionInfo;
            //if (string.IsNullOrEmpty(result.ConnectionPolicy.UserAgentSuffix))
            //{
            //    result = new DocumentCollectionInfo(collectionInfo)
            //    {
            //        ConnectionPolicy = { UserAgentSuffix = DefaultUserAgentSuffix },
            //    };
            //}

            return result;
        }

        internal static DocumentClient CreateDocumentClient(this DocumentCollectionInfo collectionInfo)
        {
            return new DocumentClient(
                collectionInfo.Uri,
                collectionInfo.MasterKey,
                new ConnectionPolicy(),
                collectionInfo.ConsistencyLevel);
        }

        // TODO: this is used as both self link and alt link (search for collectionSelfLink).
        //       The estimator needs to be fixed to use RID-based self link.
        internal static string GetCollectionSelfLink(this DocumentCollectionInfo collectionInfo)
        {
            return UriFactory.CreateDocumentCollectionUri(collectionInfo.DatabaseName, collectionInfo.CollectionName).ToString();
        }
    }
}