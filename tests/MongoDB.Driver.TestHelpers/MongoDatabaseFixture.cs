/* Copyright 2010-present MongoDB Inc.
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
using System.Collections.Generic;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.TestHelpers.Logging;

namespace MongoDB.Driver.Tests
{
    public class MongoDatabaseFixture : IDisposable
    {
        private static readonly string __timeStamp = DateTime.Now.ToString("MMddHHmm");

        private readonly Lazy<IMongoClient> _client;
        private readonly Lazy<IMongoDatabase> _database;
        private readonly string _databaseName = $"Tests{__timeStamp}";
        private bool _fixtureInialized;
        private readonly HashSet<string> _usedCollections = new();

        public MongoDatabaseFixture()
        {
            _client = new Lazy<IMongoClient>(CreateClient);
            _database = new Lazy<IMongoDatabase>(CreateDatabase);

            var logCategoriesToExclude = new[]
            {
                "MongoDB.Command",
                "MongoDB.Connection"
            };

            LogsAccumulator = new XUnitOutputAccumulator(logCategoriesToExclude);
        }

        public IMongoClient Client => _client.Value;
        public IMongoDatabase Database => _database.Value;

        internal XUnitOutputAccumulator LogsAccumulator { get; }

        public virtual void Dispose()
        {
            var client = _client.IsValueCreated ? _client.Value : null;
            var database = _database.IsValueCreated ? _database.Value : null;

            if (database != null)
            {
                foreach (var collection in _usedCollections)
                {
                    database.DropCollection(collection);
                }
            }

            client?.Dispose();
        }

        public virtual IMongoCollection<TDocument> CreateCollection<TDocument>(string collectionName = null)
        {
            if (string.IsNullOrEmpty(collectionName))
            {
                var stack = new System.Diagnostics.StackTrace();
                var frame = stack.GetFrame(1); // skip 1 frame to get the calling method info
                var method = frame.GetMethod();
                collectionName = $"{method.DeclaringType.Name}_{method.Name}";
            }

            Database.DropCollection(collectionName);
            _usedCollections.Add(collectionName);

            return Database.GetCollection<TDocument>(collectionName);
        }

        protected virtual void ConfigureMongoClientSettings(MongoClientSettings settings)
        {
            settings.LoggingSettings = new LoggingSettings(new XUnitLoggerFactory(LogsAccumulator), 10000); // Spec test require larger truncation default
        }

        protected virtual IMongoClient CreateClient()
        {
            var clientSettings = DriverTestConfiguration.GetClientSettings();
            ConfigureMongoClientSettings(clientSettings);
            return new MongoClient(clientSettings);
        }

        protected virtual IMongoDatabase CreateDatabase()
        {
            return Client.GetDatabase(_databaseName);
        }

        internal void Initialize()
        {
            if (!_fixtureInialized)
            {
                InitializeFixture();
                _fixtureInialized = true;
            }

            InitializeTestCase();
        }

        protected virtual void InitializeFixture()
        {}

        protected virtual void InitializeTestCase()
        {}
    }
}
