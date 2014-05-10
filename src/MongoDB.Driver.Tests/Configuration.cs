/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests
{
    /// <summary>
    /// A static class to handle online test configuration.
    /// </summary>
    public static class Configuration
    {
        // static constructor
        static Configuration()
        {
            var connectionString = Environment.GetEnvironmentVariable("CSharpDriverTestsConnectionString")
                ?? "mongodb://localhost/?w=1"; 

            var mongoUrl = new MongoUrl(connectionString);
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
            if (!clientSettings.WriteConcern.Enabled)
            {
                clientSettings.WriteConcern.W = 1; // ensure WriteConcern is enabled regardless of what the URL says
            }

            TestClient = new MongoClient(clientSettings);
            TestServer = TestClient.GetServer();
            TestDatabase = TestServer.GetDatabase(mongoUrl.DatabaseName ?? "csharpdriverunittests");
            TestCollection = TestDatabase.GetCollection("testcollection");

            // connect early so BuildInfo will be populated
            TestServer.Connect();
            var isMasterResult = TestDatabase.RunCommand("isMaster").Response;
            BsonValue setName = null;
            if (isMasterResult.TryGetValue("setName", out setName))
            {
                TestServerIsReplicaSet = true;
            }
        }

        // public static properties
        /// <summary>
        /// Gets the test client.
        /// </summary>
        public static MongoClient TestClient { get; private set; }

        /// <summary>
        /// Gets the test collection.
        /// </summary>
        public static MongoCollection<BsonDocument> TestCollection { get; private set; }

        /// <summary>
        /// Gets the test database.
        /// </summary>
        public static MongoDatabase TestDatabase { get; private set; }

        /// <summary>
        /// Gets the test server.
        /// </summary>
        public static MongoServer TestServer { get; private set; }

        /// <summary>
        /// Gets whether the tage MongoDB is a replica set.
        /// </summary>
        public static bool TestServerIsReplicaSet { get; private set; }

        // public static methods
        /// <summary>
        /// Gets the test collection with a default document type of T.
        /// </summary>
        /// <typeparam name="T">The default document type.</typeparam>
        /// <returns>The collection.</returns>
        public static MongoCollection<T> GetTestCollection<T>()
        {
            return TestDatabase.GetCollection<T>(TestCollection.Name);
        }
    }
}
