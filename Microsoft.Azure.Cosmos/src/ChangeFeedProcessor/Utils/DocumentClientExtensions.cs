//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  Licensed under the MIT license.
//----------------------------------------------------------------

namespace Microsoft.Azure.Documents.ChangeFeedProcessor.Utils
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Internal;

    internal static class DocumentClientExtensions
    {
        public static async Task<Document> TryGetDocumentAsync(
            this DocumentClient client,
            Uri documentUri,
            RequestOptions requestOptions = null)
        {
            try
            {
                IResourceResponse<Document> response = await client.ReadDocumentAsync(documentUri, requestOptions).ConfigureAwait(false);
                return response.Resource;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            return null;
        }

        public static async Task<Document> TryCreateDocumentAsync(this DocumentClient client, string collectionLink, object document)
        {
            try
            {
                IResourceResponse<Document> response = await client.CreateDocumentAsync(collectionLink, document).ConfigureAwait(false);
                return response.Resource;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                return null;    // Ignore -- document already exists.
            }
        }

        public static async Task<Document> TryDeleteDocumentAsync(
            this DocumentClient client,
            Uri documentUri,
            RequestOptions requestOptions = null)
        {
            try
            {
                IResourceResponse<Document> response = await client.DeleteDocumentAsync(documentUri, requestOptions).ConfigureAwait(false);
                return response.Resource;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;    // Ignore -- document not found.
            }
        }

        public static async Task<CosmosContainerSettings> GetDocumentCollectionAsync(
            this DocumentClient client,
            DocumentCollectionInfo collectionInfo)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(collectionInfo.DatabaseName, collectionInfo.CollectionName);
            IResourceResponse<CosmosContainerSettings> response =
                await client.ReadDocumentCollectionAsync(collectionUri, new RequestOptions()).ConfigureAwait(false);
            return response.Resource;
        }
    }
}