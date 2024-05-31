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
using System.Threading;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests;

namespace MongoDB.Driver.TestHelpers
{
    public abstract class TemporaryCollectionFixture<TDocument> : IDisposable
    {
        private readonly TemporaryDatabaseFixture _temporaryDatabaseFixture;
        private readonly string _collectionName;
        private readonly Lazy<bool> _collectionInitializer;

        protected TemporaryCollectionFixture(string collectionName = null)
        {
            _temporaryDatabaseFixture = new TemporaryDatabaseFixture();
            _collectionInitializer = new Lazy<bool>(() =>
            {
                InitializeCollection();
                return true;
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            _collectionName = collectionName ?? GetCollectionName();
            if (string.IsNullOrEmpty(_collectionName))
            {
                throw new ArgumentNullException(nameof(collectionName) , "Cannot resolve the collection name. Try to specify the parameter explicitly");
            }
        }

        public void Dispose()
        {
            _temporaryDatabaseFixture.Dispose();
        }

        public string CollectionName => _collectionName;

        public IMongoClient GetClient(LinqProvider provider)
            => _temporaryDatabaseFixture.GetClient(provider);

        public IMongoCollection<TDocument> GetCollection(LinqProvider provider = LinqProvider.V3)
        {
            EnsureCollectionInitialized();
            return GetDatabase(provider).GetCollection<TDocument>(CollectionName);
        }

        public IMongoDatabase GetDatabase(LinqProvider provider = LinqProvider.V3)
            => _temporaryDatabaseFixture.GetDatabase(provider);

        protected abstract IEnumerable<TDocument> GetInitialData();

        protected virtual void InitializeCollection()
        {
            var initialData = GetInitialData();
            if (initialData != null)
            {
                var collection = _temporaryDatabaseFixture.GetCollection<TDocument>(CollectionName);
                collection.InsertMany(initialData);
            }
        }

        private void EnsureCollectionInitialized()
        {
            _ = _collectionInitializer.Value;
        }

        private string GetCollectionName()
        {
            var currentType = GetType();
            return currentType.DeclaringType?.Name;
        }
    }
}
