﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.SDK.EmulatorTests
{
    using System;
    using System.IO;
    using System.Net;
    using Microsoft.Azure.Cosmos.Internal;
    using Microsoft.Azure.Cosmos.Services.Management.Tests.LinqProviderTests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class ClientTests
    {

        [TestMethod]
        public void ResourceResponseStreamingTest()
        {
            using (DocumentClient client = TestCommon.CreateClient(true))
            {
                PartitionKeyDefinition partitionKeyDefinition = new PartitionKeyDefinition { Paths = new System.Collections.ObjectModel.Collection<string>(new[] { "/pk" }), Kind = PartitionKind.Hash };
                CosmosDatabaseSettings db = client.CreateDatabaseAsync(new CosmosDatabaseSettings() { Id = Guid.NewGuid().ToString() }).Result.Resource;
                CosmosContainerSettings coll = TestCommon.CreateCollectionAsync(client, db, new CosmosContainerSettings() { Id = Guid.NewGuid().ToString() , PartitionKey = partitionKeyDefinition }).Result;
                ResourceResponse<Document> doc = client.CreateDocumentAsync(coll.SelfLink, new Document() { Id = Guid.NewGuid().ToString() }).Result;

                Assert.AreEqual(doc.ResponseStream.Position, 0);

                StreamReader streamReader = new StreamReader(doc.ResponseStream);
                string text = streamReader.ReadToEnd();

                Assert.AreEqual(doc.ResponseStream.Position, doc.ResponseStream.Length);

                try
                {
                    doc.Resource.ToString();
                    Assert.Fail("Deserializing Resource here should throw exception since the stream was already read");
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine("Expected exception while deserializing Resource: " + ex.Message);
                }
            }
        }

        [TestMethod]
        public void TestEtagOnUpsertOperationForHttpsClient()
        {
            using (DocumentClient client = TestCommon.CreateClient(false, Protocol.Https))
            {
                this.TestEtagOnUpsertOperation(client);
            }
        }

        [TestMethod]
        public void TestEtagOnUpsertOperationForGatewayClient()
        {
            using (DocumentClient client = TestCommon.CreateClient(true))
            {
                this.TestEtagOnUpsertOperation(client);
            }
        }

        [TestMethod]
        public void TestEtagOnUpsertOperationForDirectTCPClient()
        {
            using (DocumentClient client = TestCommon.CreateClient(false, Protocol.Tcp))
            {
                this.TestEtagOnUpsertOperation(client);
            }
        }

        internal void TestEtagOnUpsertOperation(DocumentClient client)
        {
            PartitionKeyDefinition partitionKeyDefinition = new PartitionKeyDefinition { Paths = new System.Collections.ObjectModel.Collection<string>(new[] { PartitionKey.SystemPartitionKeyPath }), Kind = PartitionKind.Hash };
            CosmosDatabaseSettings db = client.CreateDatabaseAsync(new CosmosDatabaseSettings() { Id = Guid.NewGuid().ToString() }).Result.Resource;
            CosmosContainerSettings coll = TestCommon.CreateCollectionAsync(client, db, new CosmosContainerSettings() { Id = Guid.NewGuid().ToString() , PartitionKey = partitionKeyDefinition}).Result;

            LinqGeneralBaselineTests.Book myBook = new LinqGeneralBaselineTests.Book();
            myBook.Id = Guid.NewGuid().ToString();
            myBook.Title = "Azure DocumentDB 101";

            Document doc = client.CreateDocumentAsync(coll, myBook).Result.Resource;

            myBook.Title = "Azure DocumentDB 201";
            client.ReplaceDocumentAsync(doc.SelfLink, myBook).Wait();

            AccessCondition condition = new AccessCondition();
            condition.Type = AccessConditionType.IfMatch;
            condition.Condition = doc.ETag;

            RequestOptions requestOptions = new RequestOptions();
            requestOptions.AccessCondition = condition;

            myBook.Title = "Azure DocumentDB 301";

            try
            {
                client.UpsertDocumentAsync(coll.SelfLink, myBook, requestOptions).Wait();
                Assert.Fail("Upsert Document should fail since the Etag is not matching.");
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException as DocumentClientException;
                Assert.IsTrue(innerException.StatusCode == HttpStatusCode.PreconditionFailed, "Invalid status code");
            }
        }

        [TestMethod]
        public void SqlQuerySpecSerializationTest()
        {
            Action<string, SqlQuerySpec> verifyJsonSerialization = (expectedText, query) =>
            {
                string actualText = JsonConvert.SerializeObject(query);

                Assert.AreEqual(expectedText, actualText);

                SqlQuerySpec otherQuery = JsonConvert.DeserializeObject<SqlQuerySpec>(actualText);
                string otherText = JsonConvert.SerializeObject(otherQuery);
                Assert.AreEqual(expectedText, otherText);
            };

            Action<string> verifyJsonSerializationText = (text) =>
            {
                SqlQuerySpec query = JsonConvert.DeserializeObject<SqlQuerySpec>(text);
                string otherText = JsonConvert.SerializeObject(query);

                Assert.AreEqual(text, otherText);
            };

            // Verify serialization 
            verifyJsonSerialization("{\"query\":null}", new SqlQuerySpec());
            verifyJsonSerialization("{\"query\":\"SELECT 1\"}", new SqlQuerySpec("SELECT 1"));
            verifyJsonSerialization("{\"query\":\"SELECT 1\",\"parameters\":[{\"name\":null,\"value\":null}]}",
                new SqlQuerySpec()
                {
                    QueryText = "SELECT 1",
                    Parameters = new SqlParameterCollection() { new SqlParameter() }
                });

            verifyJsonSerialization("{\"query\":\"SELECT 1\",\"parameters\":[" +
                    "{\"name\":\"@p1\",\"value\":5}" +
                "]}",
                new SqlQuerySpec()
                {
                    QueryText = "SELECT 1",
                    Parameters = new SqlParameterCollection() { new SqlParameter("@p1", 5) }
                });
            verifyJsonSerialization("{\"query\":\"SELECT 1\",\"parameters\":[" +
                    "{\"name\":\"@p1\",\"value\":5}," +
                    "{\"name\":\"@p1\",\"value\":true}" +
                "]}",
                new SqlQuerySpec()
                {
                    QueryText = "SELECT 1",
                    Parameters = new SqlParameterCollection() { new SqlParameter("@p1", 5), new SqlParameter("@p1", true) }
                });
            verifyJsonSerialization("{\"query\":\"SELECT 1\",\"parameters\":[" +
                    "{\"name\":\"@p1\",\"value\":\"abc\"}" +
                "]}",
                new SqlQuerySpec()
                {
                    QueryText = "SELECT 1",
                    Parameters = new SqlParameterCollection() { new SqlParameter("@p1", "abc") }
                });
            verifyJsonSerialization("{\"query\":\"SELECT 1\",\"parameters\":[" +
                    "{\"name\":\"@p1\",\"value\":[1,2,3]}" +
                "]}",
                new SqlQuerySpec()
                {
                    QueryText = "SELECT 1",
                    Parameters = new SqlParameterCollection() { new SqlParameter("@p1", new int[] { 1, 2, 3 }) }
                });
            verifyJsonSerialization("{\"query\":\"SELECT 1\",\"parameters\":[" +
                    "{\"name\":\"@p1\",\"value\":{\"a\":[1,2,3]}}" +
                "]}",
                new SqlQuerySpec()
                {
                    QueryText = "SELECT 1",
                    Parameters = new SqlParameterCollection() { new SqlParameter("@p1", JObject.Parse("{\"a\":[1,2,3]}")) }
                });
            verifyJsonSerialization("{\"query\":\"SELECT 1\",\"parameters\":[" +
                    "{\"name\":\"@p1\",\"value\":{\"a\":[1,2,3]}}" +
                "]}",
                new SqlQuerySpec()
                {
                    QueryText = "SELECT 1",
                    Parameters = new SqlParameterCollection() { new SqlParameter("@p1", new JRaw("{\"a\":[1,2,3]}")) }
                });

            // Verify roundtrips
            verifyJsonSerializationText("{\"query\":null}");
            verifyJsonSerializationText("{\"query\":\"SELECT 1\"}");
            verifyJsonSerializationText(
                "{" +
                    "\"query\":\"SELECT 1\"," +
                    "\"parameters\":[" +
                        "{\"name\":null,\"value\":null}" +
                    "]" +
                "}");
            verifyJsonSerializationText(
                "{" +
                    "\"query\":\"SELECT 1\"," +
                    "\"parameters\":[" +
                        "{\"name\":\"@p1\",\"value\":null}" +
                    "]" +
                "}");
            verifyJsonSerializationText(
                "{" +
                    "\"query\":\"SELECT 1\"," +
                    "\"parameters\":[" +
                        "{\"name\":\"@p1\",\"value\":true}" +
                    "]" +
                "}");
            verifyJsonSerializationText(
                "{" +
                    "\"query\":\"SELECT 1\"," +
                    "\"parameters\":[" +
                        "{\"name\":\"@p1\",\"value\":false}" +
                    "]" +
                "}");
            verifyJsonSerializationText(
                "{" +
                    "\"query\":\"SELECT 1\"," +
                    "\"parameters\":[" +
                        "{\"name\":\"@p1\",\"value\":123}" +
                    "]" +
                "}");
            verifyJsonSerializationText(
                "{" +
                    "\"query\":\"SELECT 1\"," +
                    "\"parameters\":[" +
                        "{\"name\":\"@p1\",\"value\":\"abc\"}" +
                    "]" +
                "}");
            verifyJsonSerializationText(
                "{" +
                    "\"query\":\"SELECT 1\"," +
                    "\"parameters\":[" +
                        "{\"name\":\"@p1\",\"value\":{\"a\":[1,2,\"abc\"]}}" +
                    "]" +
                "}");
        }
    }

    internal static class StringHelper
    {
        internal static string EscapeForSQL(this string input)
        {
            return input.Replace("'", "\\'").Replace("\"", "\\\"");
        }
    }
}
