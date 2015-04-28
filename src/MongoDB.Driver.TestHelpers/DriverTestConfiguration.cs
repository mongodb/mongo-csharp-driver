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
    public static class DriverTestConfiguration
    {
        // private static fields
        private static MongoClient __client;
        private static CollectionNamespace __collectionNamespace;
        private static DatabaseNamespace __databaseNamespace;

        // static constructor
        static DriverTestConfiguration()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();

            var mongoUrl = new MongoUrl(connectionString);
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
            if (!clientSettings.WriteConcern.IsAcknowledged)
            {
                clientSettings.WriteConcern = WriteConcern.Acknowledged; // ensure WriteConcern is enabled regardless of what the URL says
            }

            clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(500);

            __client = new MongoClient(clientSettings);
            __databaseNamespace = mongoUrl.DatabaseName == null ? CoreTestConfiguration.DatabaseNamespace : new DatabaseNamespace(mongoUrl.DatabaseName);
            __collectionNamespace = new CollectionNamespace(__databaseNamespace, "testcollection");
        }

        // public static properties
        /// <summary>
        /// Gets the test client.
        /// </summary>
        public static MongoClient Client
        {
            get { return __client; }
        }

        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public static CollectionNamespace CollectionNamespace
        {
            get { return __collectionNamespace; }
        }

        /// <summary>
        /// Gets the database namespace.
        /// </summary>
        /// <value>
        /// The database namespace.
        /// </value>
        public static DatabaseNamespace DatabaseNamespace
        {
            get { return __databaseNamespace; }
        }
    }
}
