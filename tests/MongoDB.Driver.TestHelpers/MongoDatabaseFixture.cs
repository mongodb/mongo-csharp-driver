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

namespace MongoDB.Driver.Tests
{
    public class MongoDatabaseFixture : IDisposable
    {
        private static readonly string __timeStamp = DateTime.Now.ToString("MMddHHmm");

        private readonly Lazy<IMongoClient> _client;
        private readonly Lazy<IMongoDatabase> _database;
        private readonly string _databaseName = $"CSTests{__timeStamp}";
        private bool _fixtureInialized;
        private readonly HashSet<string> _usedCollections = new();

        public MongoDatabaseFixture()
        {
            _client = new Lazy<IMongoClient>(CreateClient);
            _database = new Lazy<IMongoDatabase>(CreateDatabase);
        }

        public IMongoClient Client => _client.Value;
        public IMongoDatabase Database => _database.Value;

        public virtual void Dispose()
        {
            var database = _database.IsValueCreated ? _database.Value : null;
            if (database != null)
            {
                foreach (var collection in _usedCollections)
                {
                    database.DropCollection(collection);
                }
            }
        }

        protected IMongoCollection<TDocument> CreateCollection<TDocument>(string collectionName)
        {
            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ArgumentException($"{nameof(collectionName)} should be non-empty string.", nameof(collectionName));
            }

            Database.DropCollection(collectionName);
            _usedCollections.Add(collectionName);

            return Database.GetCollection<TDocument>(collectionName);
        }

        protected virtual IMongoClient CreateClient()
            => DriverTestConfiguration.Client;

        protected virtual IMongoDatabase CreateDatabase()
        {
            return Client.GetDatabase(_databaseName);
        }

        internal void BeforeTestCase()
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
