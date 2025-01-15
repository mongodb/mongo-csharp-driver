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
        private static readonly string __timeStamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        private readonly string _databaseName = $"CSharpDriver-{__timeStamp}";
        private readonly HashSet<string> _usedCollections = new();
        private IMongoClient _client;

        public MongoDatabaseFixture()
        {
            var logCategoriesToExclude = new[]
            {
                "MongoDB.Command",
                "MongoDB.Connection"
            };

            LogsAccumulator = new XUnitOutputAccumulator(logCategoriesToExclude);
        }

        internal XUnitOutputAccumulator LogsAccumulator { get; }

        public virtual void Dispose()
        {
            if (_client == null)
            {
                return;
            }

            var database = GetDatabase();
            foreach (var collection in _usedCollections)
            {
                database.DropCollection(collection);
            }

            var client = _client;
            _client = null;
            client.Dispose();
        }

        public IMongoClient GetClient()
            => _client;

        public virtual IMongoDatabase GetDatabase()
            => GetClient().GetDatabase(_databaseName);

        public virtual IMongoCollection<T> GetCollection<T>(string collectionName = null)
        {
            if (string.IsNullOrEmpty(collectionName))
            {
                var stack = new System.Diagnostics.StackTrace();
                var frame = stack.GetFrame(2); // skip 2 frames to get the calling method info (this method and IntegrationTest method)
                var method = frame.GetMethod();
                collectionName = $"{method.DeclaringType.Name}.{method.Name}_{typeof(T).Name}";
            }

            var db = GetDatabase();
            db.DropCollection(collectionName);
            _usedCollections.Add(collectionName);
            return db.GetCollection<T>(collectionName);
        }

        protected virtual void ConfigureMongoClient(MongoClientSettings settings)
        {
            settings.LoggingSettings = new LoggingSettings(new XUnitLoggerFactory(LogsAccumulator), 10000); // Spec test require larger truncation default
        }

        internal void Initialize()
        {
            if (_client == null)
            {
                var clientSettings = DriverTestConfiguration.GetClientSettings();
                ConfigureMongoClient(clientSettings);
                _client = new MongoClient(clientSettings);
                InitializeFixture();
            }

            InitializeTestCase();
        }

        protected virtual void InitializeFixture()
        {}

        protected virtual void InitializeTestCase()
        {}
    }
}
