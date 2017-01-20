/* Copyright 2010-2015 MongoDB Inc.
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
    public static class LegacyTestConfiguration
    {
        // private static fields
        private static MongoServer __server;
        private static MongoDatabase __database;
        private static MongoCollection<BsonDocument> __collection;
        private static bool __isReplicaSet;

        // static constructor
        static LegacyTestConfiguration()
        {
#pragma warning disable 618
            __server = DriverTestConfiguration.Client.GetServer();
#pragma warning restore
            __database = __server.GetDatabase(CoreTestConfiguration.DatabaseNamespace.DatabaseName);
            __collection = __database.GetCollection("testcollection");

            // connect early so BuildInfo will be populated
            __server.Connect();
            var isMasterResult = __database.RunCommand("isMaster").Response;
            BsonValue setName = null;
            if (isMasterResult.TryGetValue("setName", out setName))
            {
                __isReplicaSet = true;
            }
        }

        // public static properties
        /// <summary>
        /// Gets the test collection.
        /// </summary>
        public static MongoCollection<BsonDocument> Collection
        {
            get { return __collection; }
        }

        /// <summary>
        /// Gets the test database.
        /// </summary>
        public static MongoDatabase Database
        {
            get { return __database; }
        }

        /// <summary>
        /// Gets the test server.
        /// </summary>
        public static MongoServer Server
        {
            get { return __server; }
        }

        /// <summary>
        /// Gets whether the tage MongoDB is a replica set.
        /// </summary>
        public static bool IsReplicaSet
        {
            get { return __isReplicaSet; }
        }

        // public static methods
        /// <summary>
        /// Gets the test collection with a default document type of T.
        /// </summary>
        /// <typeparam name="T">The default document type.</typeparam>
        /// <returns>The collection.</returns>
        public static MongoCollection<T> GetCollection<T>()
        {
            return __database.GetCollection<T>(__collection.Name);
        }

        public static void StartReplication(MongoServerInstance secondary)
        {
            using (__server.RequestStart(secondary))
            {
                var adminDatabaseSettings = new MongoDatabaseSettings { ReadPreference = ReadPreference.Secondary };
                var adminDatabase = __server.GetDatabase("admin", adminDatabaseSettings);
                var command = new CommandDocument
                {
                    { "configureFailPoint", "rsSyncApplyStop"},
                    { "mode", "off" }
                };
                adminDatabase.RunCommandAs<CommandResult>(command, ReadPreference.Secondary);
            }
        }

        public static IDisposable StopReplication(MongoServerInstance secondary)
        {
            using (__server.RequestStart(secondary))
            {
                var adminDatabaseSettings = new MongoDatabaseSettings { ReadPreference = ReadPreference.Secondary };
                var adminDatabase = __server.GetDatabase("admin", adminDatabaseSettings);
                var command = new CommandDocument
                {
                    { "configureFailPoint", "rsSyncApplyStop"},
                    { "mode", "alwaysOn" }
                };
                adminDatabase.RunCommandAs<CommandResult>(command, ReadPreference.Secondary);

                return new ReplicationRestarter(secondary);
            }
        }

        // nested types
        private class ReplicationRestarter : IDisposable
        {
            MongoServerInstance _secondary;

            public ReplicationRestarter(MongoServerInstance secondary)
            {
                _secondary = secondary;
            }

            public void Dispose()
            {
                LegacyTestConfiguration.StartReplication(_secondary);
            }
        }
    }
}
