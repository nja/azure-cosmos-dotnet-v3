﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.SDK.EmulatorTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Collections;
    using Microsoft.Azure.Cosmos.Internal;
    using Microsoft.Azure.Cosmos.Routing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HeadersValidationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext textContext)
        {
            DocumentClientSwitchLinkExtension.Reset("HeadersValidationTests");
        }

        [TestInitialize]
        public void Startup()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Tcp);
            var client = TestCommon.CreateClient(true);
            TestCommon.DeleteAllDatabasesAsync(client).Wait();
        }

        [TestMethod]
        public void ValidatePageSizeHttps()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Https);
            var client = TestCommon.CreateClient(true, Protocol.Https);
            ValidatePageSize(client);
        }

        [TestMethod]
        public void ValidatePageSizeRntbd()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Tcp);
            var client = TestCommon.CreateClient(true, Protocol.Tcp);
            ValidatePageSize(client);
        }

        [TestMethod]
        public void ValidatePageSizeGatway()
        {
            var client = TestCommon.CreateClient(true);
            ValidatePageSize(client);
        }

        private void ValidatePageSize(DocumentClient client)
        {
            // Invalid parsing
            INameValueCollection headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.PageSize, "\"Invalid header type\"");

            try
            {
                ReadDatabaseFeedRequest(client, headers);
                Assert.Fail("Should throw an exception");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            headers = new StringKeyValueCollection();
            headers.Add("pageSize", "\"Invalid header type\"");

            try
            {
                ReadFeedScript(client, headers);
                Assert.Fail("Should throw an exception");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            // Invalid value
            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.PageSize, "-2");

            try
            {
                ReadDatabaseFeedRequest(client, headers);
                Assert.Fail("Should throw an exception");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.PageSize, Int64.MaxValue.ToString(CultureInfo.InvariantCulture));

            try
            {
                ReadFeedScript(client, headers);
                Assert.Fail("Should throw an exception");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            // Valid page size
            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.PageSize, "20");
            var response = ReadDatabaseFeedRequest(client, headers);
            Assert.IsTrue(response.StatusCode == HttpStatusCode.OK);

            headers = new StringKeyValueCollection();
            headers.Add("pageSize", "20");
            var result = ReadFeedScript(client, headers);
            Assert.IsTrue(result.StatusCode == HttpStatusCode.OK);

            // dynamic page size
            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.PageSize, "-1");
            response = ReadDatabaseFeedRequest(client, headers);
            Assert.IsTrue(response.StatusCode == HttpStatusCode.OK);

            headers = new StringKeyValueCollection();
            headers.Add("pageSize", "-1");
            result = ReadFeedScript(client, headers);
            Assert.IsTrue(result.StatusCode == HttpStatusCode.OK);
        }

        [TestMethod]
        public void ValidateConsistencyLevelGateway()
        {
            var client = TestCommon.CreateClient(true);
            ValidateCosistencyLevel(client);
        }

        [TestMethod]
        public void ValidateConsistencyLevelRntbd()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Tcp);
            var client = TestCommon.CreateClient(true, Protocol.Tcp);
            ValidateCosistencyLevel(client);
        }

        [TestMethod]
        public void ValidateConsistencyLevelHttps()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Https);
            var client = TestCommon.CreateClient(true, Protocol.Https);
            ValidateCosistencyLevel(client);
        }

        private void ValidateCosistencyLevel(DocumentClient client)
        {
            CosmosContainerSettings collection = TestCommon.CreateOrGetDocumentCollection(client);

            // Value not supported
            INameValueCollection headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.ConsistencyLevel, "Not a valid value");

            try
            {
                ReadDocumentFeedRequest(client, collection.ResourceId, headers);
                Assert.Fail("Should throw an exception");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "invalid status code");
            }

            // Supported value
            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.ConsistencyLevel, ConsistencyLevel.Eventual.ToString());
            var response = ReadDocumentFeedRequest(client, collection.ResourceId, headers);
            Assert.IsTrue(response.StatusCode == HttpStatusCode.OK, "Invalid status code");
        }

        [TestMethod]
        public void ValidateIndexingDirectiveGateway()
        {
            var client = TestCommon.CreateClient(true);
            ValidateIndexingDirective(client);
        }

        [TestMethod]
        public void ValidateIndexingDirectiveRntbd()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Tcp);
            var client = TestCommon.CreateClient(true, Protocol.Tcp);
            ValidateIndexingDirective(client);
        }

        [TestMethod]
        public void ValidateIndexingDirectiveHttps()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Https);
            var client = TestCommon.CreateClient(true, Protocol.Https);
            ValidateIndexingDirective(client);
        }

        private void ValidateIndexingDirective(DocumentClient client)
        {
            // Number out of range.
            INameValueCollection headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.IndexingDirective, "\"Invalid Value\"");

            try
            {
                CreateDocumentRequest(client, headers);
                Assert.Fail("Should throw an exception");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            headers = new StringKeyValueCollection();
            headers.Add("indexAction", "\"Invalid Value\"");

            try
            {
                CreateDocumentScript(client, headers);
                Assert.Fail("Should throw an exception");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            // Valid Indexing Directive
            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.IndexingDirective, IndexingDirective.Exclude.ToString());
            var response = CreateDocumentRequest(client, headers);
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created);

            headers = new StringKeyValueCollection();
            headers.Add("indexAction", "\"exclude\"");
            var result = CreateDocumentScript(client, headers);
            Assert.IsTrue(result.StatusCode == HttpStatusCode.OK, "Invalid status code");
        }

        [TestMethod]
        public void ValidateEnableScanInQueryGateway()
        {
            var client = TestCommon.CreateClient(true);
            ValidateEnableScanInQuery(client);
        }

        [TestMethod]
        public void ValidateEnableScanInQueryRntbd()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Tcp);
            var client = TestCommon.CreateClient(true, Protocol.Tcp);
            ValidateEnableScanInQuery(client);
        }

        [TestMethod]
        public void ValidateEnableScanInQueryHttps()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Https);
            var client = TestCommon.CreateClient(true, Protocol.Https);
            ValidateEnableScanInQuery(client);
        }

        private void ValidateEnableScanInQuery(DocumentClient client, bool isHttps = false)
        {
            // Value not boolean
            INameValueCollection headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.EnableScanInQuery, "Not a boolean");

            try
            {
                var response = ReadDatabaseFeedRequest(client, headers);
                if (isHttps)
                {
                    Assert.Fail("Should throw an exception");
                }
                else
                {
                    // Invalid boolean is treated as false by TCP
                    Assert.IsTrue(response.StatusCode == HttpStatusCode.OK, "Invalid status code");
                }
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            // Valid boolean
            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.EnableScanInQuery, "true");
            var response2 = ReadDatabaseFeedRequest(client, headers);
            Assert.IsTrue(response2.StatusCode == HttpStatusCode.OK, "Invalid status code");
        }

        [TestMethod]
        public void ValidateEnableLowPrecisionOrderByGateway()
        {
            var client = TestCommon.CreateClient(true);
            ValidateEnableLowPrecisionOrderBy(client);
        }

        private void ValidateEnableLowPrecisionOrderBy(DocumentClient client, bool isHttps = false)
        {
            // Value not boolean
            INameValueCollection headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.EnableLowPrecisionOrderBy, "Not a boolean");

            var document = CreateDocumentRequest(client, new StringKeyValueCollection()).GetResource<Document>();
            try
            {
                var response = ReadDocumentRequest(client, document, headers);
                if (isHttps)
                {
                    Assert.Fail("Should throw an exception");
                }
                else
                {
                    // Invalid boolean is treated as false by TCP"
                    Assert.IsTrue(response.StatusCode == HttpStatusCode.OK, "Invalid status code");
                }
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            // Valid boolean
            document = CreateDocumentRequest(client, new StringKeyValueCollection()).GetResource<Document>();
            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.EnableLowPrecisionOrderBy, "true");
            var response2 = ReadDocumentRequest(client, document, headers);
            Assert.IsTrue(response2.StatusCode == HttpStatusCode.OK, "Invalid status code");
        }

        [TestMethod]
        public void ValidateEmitVerboseTracesInQueryGateway()
        {
            var client = TestCommon.CreateClient(true);
            ValidateEmitVerboseTracesInQuery(client);
        }

        [TestMethod]
        public void ValidateEmitVerboseTracesInQueryRntbd()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Tcp);
            var client = TestCommon.CreateClient(true, Protocol.Tcp);
            ValidateEmitVerboseTracesInQuery(client);
        }

        [TestMethod]
        [Ignore /* This test fails without an understandable reason, it expects an exception that doesn't happen */]
        public void ValidateEmitVerboseTracesInQueryHttps()
        {
            var client = TestCommon.CreateClient(false, Protocol.Https);
            ValidateEmitVerboseTracesInQuery(client, true);
        }

        private void ValidateEmitVerboseTracesInQuery(DocumentClient client, bool isHttps = false)
        {
            // Value not boolean
            INameValueCollection headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.EmitVerboseTracesInQuery, "Not a boolean");

            try
            {
                var response = ReadDatabaseFeedRequest(client, headers);
                if (isHttps)
                {
                    Assert.Fail("Should throw an exception");
                }
                else
                {
                    // Invalid boolean is treated as false by TCP
                    Assert.IsTrue(response.StatusCode == HttpStatusCode.OK, "Invalid status code");
                }
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.BadRequest, "Invalid status code");
            }

            // Valid boolean
            headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.EmitVerboseTracesInQuery, "true");
            var response2 = ReadDatabaseFeedRequest(client, headers);
            Assert.IsTrue(response2.StatusCode == HttpStatusCode.OK, "Invalid status code");
        }

        [TestMethod]
        public void ValidateIfNonMatchGateway()
        {
            var client = TestCommon.CreateClient(true);
            ValidateIfNonMatch(client);

        }
        [TestMethod]
        public void ValidateIfNonMatchHttps()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Https);
            var client = TestCommon.CreateClient(true, Protocol.Https);
            ValidateIfNonMatch(client);
        }

        [TestMethod]
        public void ValidateIfNonMatchRntbd()
        {
            //var client = TestCommon.CreateClient(false, Protocol.Tcp);
            var client = TestCommon.CreateClient(true, Protocol.Tcp);
            ValidateIfNonMatch(client);
        }

        [TestMethod]
        public void ValidateCustomUserAgentHeader()
        {
            const string suffix = " MyCustomUserAgent/1.0";
            ConnectionPolicy policy = new ConnectionPolicy();
            policy.UserAgentSuffix = suffix;
            var expectedUserAgent = UserAgentContainer.baseUserAgent + suffix;
            Assert.AreEqual(expectedUserAgent, policy.UserAgentContainer.UserAgent);

            byte[] expectedUserAgentUTF8 = Encoding.UTF8.GetBytes(expectedUserAgent);
            CollectionAssert.AreEqual(expectedUserAgentUTF8, policy.UserAgentContainer.UserAgentUTF8);
        }

        [TestMethod]
        public void ValidateVersionHeader()
        {
            string correctVersion = HttpConstants.Versions.CurrentVersion;
            try
            {
                DocumentClient client = TestCommon.CreateClient(true);
                var db = client.CreateDatabaseAsync(new CosmosDatabaseSettings() { Id = Guid.NewGuid().ToString() }).Result.Resource;
                var coll = client.CreateDocumentCollectionAsync(db.SelfLink, new CosmosContainerSettings() { Id = Guid.NewGuid().ToString() }).Result.Resource;
                var doc = client.CreateDocumentAsync(coll.SelfLink, new Document()).Result.Resource;
                client = TestCommon.CreateClient(true);
                doc = client.CreateDocumentAsync(coll.SelfLink, new Document()).Result.Resource;
                HttpConstants.Versions.CurrentVersion = "2015-01-01";
                client = TestCommon.CreateClient(true);
                try
                {
                    doc = client.CreateDocumentAsync(coll.SelfLink, new Document()).Result.Resource;
                    Assert.Fail("Should have faild because of version error");
                }
                catch (AggregateException exception)
                {
                    var dce = exception.InnerException as DocumentClientException;
                    if (dce != null)
                    {
                        Assert.AreEqual(dce.StatusCode, HttpStatusCode.BadRequest);
                    }
                    else
                    {
                        Assert.Fail("Should have faild because of version error with DocumentClientException BadRequest");
                    }
                }
            }
            finally
            {
                HttpConstants.Versions.CurrentVersion = correctVersion;
            }
        }

        [TestMethod]
        public void ValidateCurrentWriteQuorumAndReplicaSetHeader()
        {
            DocumentClient client = TestCommon.CreateClient(false);
            CosmosDatabaseSettings db = null;
            try
            {
                var dbResource = client.CreateDatabaseAsync(new CosmosDatabaseSettings() { Id = Guid.NewGuid().ToString() }).Result;
                db = dbResource.Resource;
                var coll = client.CreateDocumentCollectionAsync(db, new CosmosContainerSettings() { Id = Guid.NewGuid().ToString() }).Result.Resource;
                var docResult = client.CreateDocumentAsync(coll, new Document() { Id = Guid.NewGuid().ToString() }).Result;
                Assert.IsTrue(int.Parse(docResult.ResponseHeaders[WFConstants.BackendHeaders.CurrentWriteQuorum], CultureInfo.InvariantCulture) > 0);
                Assert.IsTrue(int.Parse(docResult.ResponseHeaders[WFConstants.BackendHeaders.CurrentReplicaSetSize], CultureInfo.InvariantCulture) > 0);
            }
            finally
            {
                client.DeleteDatabaseAsync(db).Wait();
            }
        }

        [TestMethod]
        public async Task ValidateCollectionIndexProgressHeadersGateway()
        {
            var client = TestCommon.CreateClient(true);
            await ValidateCollectionIndexProgressHeaders(client);
        }

        [TestMethod]
        public async Task ValidateCollectionIndexProgressHeadersHttps()
        {
            var client = TestCommon.CreateClient(false, Protocol.Https);
            await ValidateCollectionIndexProgressHeaders(client);
        }

        [TestMethod]
        public async Task ValidateCollectionIndexProgressHeadersRntbd()
        {
            var client = TestCommon.CreateClient(false, Protocol.Tcp);
            await ValidateCollectionIndexProgressHeaders(client);
        }

        private async Task ValidateCollectionIndexProgressHeaders(DocumentClient client)
        {
            CosmosDatabaseSettings db = (await client.CreateDatabaseAsync(new CosmosDatabaseSettings() { Id = Guid.NewGuid().ToString() })).Resource;

            try
            {
                var lazyCollection = new CosmosContainerSettings() { Id = Guid.NewGuid().ToString() };
                lazyCollection.IndexingPolicy.IndexingMode = IndexingMode.Lazy;
                lazyCollection = (await client.CreateDocumentCollectionAsync(db, lazyCollection)).Resource;

                var consistentCollection = new CosmosContainerSettings() { Id = Guid.NewGuid().ToString() };
                consistentCollection.IndexingPolicy.IndexingMode = IndexingMode.Consistent;
                consistentCollection = (await client.CreateDocumentCollectionAsync(db, consistentCollection)).Resource;

                var noneIndexCollection = new CosmosContainerSettings() { Id = Guid.NewGuid().ToString() };
                noneIndexCollection.IndexingPolicy.Automatic = false;
                noneIndexCollection.IndexingPolicy.IndexingMode = IndexingMode.None;
                noneIndexCollection = (await client.CreateDocumentCollectionAsync(db, noneIndexCollection)).Resource;

                var doc = new Document() { Id = Guid.NewGuid().ToString() };
                await client.CreateDocumentAsync(lazyCollection, doc);
                await client.CreateDocumentAsync(consistentCollection, doc);
                await client.CreateDocumentAsync(noneIndexCollection, doc);


                // Lazy-indexing collection.
                {
                    var collectionResponse = await client.ReadDocumentCollectionAsync(lazyCollection);
                    Assert.IsTrue(int.Parse(collectionResponse.ResponseHeaders[HttpConstants.HttpHeaders.CollectionLazyIndexingProgress], CultureInfo.InvariantCulture) >= 0,
                        "Expect lazy indexer progress when reading lazy collection.");
                    Assert.AreEqual(100, int.Parse(collectionResponse.ResponseHeaders[HttpConstants.HttpHeaders.CollectionIndexTransformationProgress], CultureInfo.InvariantCulture),
                        "Expect reindexer progress when reading lazy collection.");
                    Assert.IsTrue(collectionResponse.LazyIndexingProgress >= 0 && collectionResponse.LazyIndexingProgress <= 100);
                    Assert.AreEqual(100, collectionResponse.IndexTransformationProgress);
                }

                // Consistent-indexing collection.
                {
                    var collectionResponse = await client.ReadDocumentCollectionAsync(consistentCollection);
                    Assert.IsFalse(collectionResponse.Headers.AllKeys().Contains(HttpConstants.HttpHeaders.CollectionLazyIndexingProgress),
                        "No lazy indexer progress when reading consistent collection.");
                    Assert.AreEqual(100, int.Parse(collectionResponse.ResponseHeaders[HttpConstants.HttpHeaders.CollectionIndexTransformationProgress], CultureInfo.InvariantCulture),
                        "Expect reindexer progress when reading consistent collection.");
                    Assert.AreEqual(-1, collectionResponse.LazyIndexingProgress);
                    Assert.AreEqual(100, collectionResponse.IndexTransformationProgress);
                }

                // None-indexing collection.
                {
                    var collectionResponse = await client.ReadDocumentCollectionAsync(noneIndexCollection);
                    Assert.IsFalse(collectionResponse.Headers.AllKeys().Contains(HttpConstants.HttpHeaders.CollectionLazyIndexingProgress),
                        "No lazy indexer progress when reading none-index collection.");
                    Assert.AreEqual(100, int.Parse(collectionResponse.ResponseHeaders[HttpConstants.HttpHeaders.CollectionIndexTransformationProgress], CultureInfo.InvariantCulture),
                        "Expect reindexer progress when reading none-index collection.");
                    Assert.AreEqual(-1, collectionResponse.LazyIndexingProgress);
                    Assert.AreEqual(100, collectionResponse.IndexTransformationProgress);
                }
            }
            finally
            {
                client.DeleteDatabaseAsync(db).Wait();
            }
        }

        private void ValidateIfNonMatch(DocumentClient client)
        {
            // Valid if-match
            var document = CreateDocumentRequest(client, new StringKeyValueCollection()).GetResource<Document>();
            var headers = new StringKeyValueCollection();
            headers.Add(HttpConstants.HttpHeaders.IfNoneMatch, document.ETag);
            var response = ReadDocumentRequest(client, document, headers);
            Assert.IsTrue(response.StatusCode == HttpStatusCode.NotModified, "Invalid status code");

            // validateInvalidIfMatch
            AccessCondition condition = new AccessCondition() { Type = AccessConditionType.IfMatch, Condition = "invalid etag" };
            try
            {
                var replacedDoc = client.ReplaceDocumentAsync(document.SelfLink, document, new RequestOptions() { AccessCondition = condition }).Result.Resource;
                Assert.Fail("should not reach here");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.PreconditionFailed, "Invalid status code");
            }
        }

        private DocumentServiceResponse ReadDocumentFeedRequest(DocumentClient client, string collectionId, INameValueCollection headers)
        {
            DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.ReadFeed, collectionId, ResourceType.Document, AuthorizationTokenType.PrimaryMasterKey, headers);
           
            Range<string> fullRange = new Range<string>(
                PartitionKeyInternal.MinimumInclusiveEffectivePartitionKey,
                PartitionKeyInternal.MaximumExclusiveEffectivePartitionKey,
                true,
                false);
            IRoutingMapProvider routingMapProvider = client.GetPartitionKeyRangeCacheAsync().Result;
            IReadOnlyList<PartitionKeyRange> ranges = routingMapProvider.TryGetOverlappingRangesAsync(collectionId, fullRange).Result;
            request.RouteTo(new PartitionKeyRangeIdentity(collectionId, ranges.First().Id));

            var response = client.ReadFeedAsync(request).Result;
            return response;
        }

        private DocumentServiceResponse ReadDatabaseFeedRequest(DocumentClient client, INameValueCollection headers)
        {
            DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.ReadFeed, null, ResourceType.Database, AuthorizationTokenType.PrimaryMasterKey, headers);
            var response = client.ReadFeedAsync(request).Result;
            return response;
        }

        private StoredProcedureResponse<string> ReadFeedScript(DocumentClient client, INameValueCollection headers)
        {
            var headersIterator = headers.AllKeys().SelectMany(headers.GetValues, (k, v) => new { key = k, value = v });
            var scriptOptions = "{";
            var headerIndex = 0;
            foreach (var header in headersIterator)
            {
                if (headerIndex != 0)
                {
                    scriptOptions += ", ";
                }

                headerIndex++;
                scriptOptions += header.key + ":" + header.value;
            }

            scriptOptions += "}";

            var script = @"function() {
                var client = getContext().getCollection();
                function callback(err, docFeed, responseOptions) {
                    if(err) throw 'Error while reading documents';
                    docFeed.forEach(function(doc, i, arr) { getContext().getResponse().appendBody(JSON.stringify(doc));  });
                };
                client.readDocuments(client.getSelfLink()," + scriptOptions + @", callback);}";

            var collection = TestCommon.CreateOrGetDocumentCollection(client);
            var sproc = new CosmosStoredProcedureSettings() { Id = Guid.NewGuid().ToString(), Body = script };
            var createdSproc = client.CreateStoredProcedureAsync(collection, sproc).Result.Resource;
            var result = client.ExecuteStoredProcedureAsync<string>(createdSproc).Result;
            return result;
        }

        private DocumentServiceResponse CreateDocumentRequest(DocumentClient client, INameValueCollection headers)
        {
            var collection = TestCommon.CreateOrGetDocumentCollection(client);
            var document = new Document() { Id = Guid.NewGuid().ToString() };
            DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Create, collection.SelfLink, document, ResourceType.Document, AuthorizationTokenType.Invalid, headers, SerializationFormattingPolicy.None);
            request.Headers[HttpConstants.HttpHeaders.PartitionKey] = PartitionKeyInternal.Empty.ToJsonString();
            var response = client.CreateAsync(request).Result;
            return response;
        }

        private StoredProcedureResponse<string> CreateDocumentScript(DocumentClient client, INameValueCollection headers)
        {
            var headersIterator = headers.AllKeys().SelectMany(headers.GetValues, (k, v) => new { key = k, value = v });
            var scriptOptions = "{";
            var headerIndex = 0;
            foreach (var header in headersIterator)
            {
                if (headerIndex != 0)
                {
                    scriptOptions += ", ";
                }

                headerIndex++;
                scriptOptions += header.key + ":" + header.value;
            }

            scriptOptions += "}";
            var guid = Guid.NewGuid().ToString();

            var script = @" function() {
                var client = getContext().getCollection();                
                client.createDocument(client.getSelfLink(), { id: Math.random() + """" }," + scriptOptions + @", function(err, docCreated, options) { 
                   if(err) throw new Error('Error while creating document: ' + err.message); 
                   else {
                     getContext().getResponse().setBody(JSON.stringify(docCreated));  
                   }
                });}";

            var collection = TestCommon.CreateOrGetDocumentCollection(client);
            var sproc = new CosmosStoredProcedureSettings() { Id = Guid.NewGuid().ToString(), Body = script };
            var createdSproc = client.CreateStoredProcedureAsync(collection, sproc).Result.Resource;
            var result = client.ExecuteStoredProcedureAsync<string>(createdSproc).Result;
            return result;
        }

        private DocumentServiceResponse ReadDocumentRequest(DocumentClient client, Document doc, INameValueCollection headers)
        {
            DocumentServiceRequest request = DocumentServiceRequest.Create(OperationType.Read, ResourceType.Document, doc.SelfLink, AuthorizationTokenType.PrimaryMasterKey, headers);
            request.Headers[HttpConstants.HttpHeaders.PartitionKey] = PartitionKeyInternal.Empty.ToJsonString();
            var retrievedDocResponse = client.ReadAsync(request).Result;
            return retrievedDocResponse;
        }
    }
}
