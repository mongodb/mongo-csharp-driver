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
using MongoDB.Driver.Tests;

namespace MongoDB.Driver.TestHelpers
{
    public abstract class CollectionFixture<TDocument> : DatabaseFixture
    {
        private readonly string _collectionName;
        private bool _collectionInitialized;

        protected CollectionFixture(string collectionName = null)
        {
            _collectionName = collectionName ?? GetCollectionName();
            if (string.IsNullOrEmpty(_collectionName))
            {
                throw new ArgumentNullException(nameof(collectionName) , "Cannot resolve the collection name. Try to specify the parameter explicitly");
            }
        }

        public string CollectionName => _collectionName;

        public virtual bool ResetOnEachGet => false;

        public override IMongoCollection<T> GetCollection<T>(Action<MongoClientSettings> configure = null, string collectionName = null)
        {
            if (!string.IsNullOrEmpty(collectionName))
            {
                throw new NotSupportedException("CollectionFixture does not support explicit collection name.");
            }

            if (ResetOnEachGet || !_collectionInitialized)
            {
                InitializeCollection();
            }

            var db = GetDatabase(configure);
            return db.GetCollection<T>(CollectionName);
        }

        protected abstract IEnumerable<TDocument> GetInitialData();

        private void InitializeCollection()
        {
            var initialData = GetInitialData();
            if (initialData != null)
            {
                var collection = base.GetCollection<TDocument>(null, CollectionName);
                collection.InsertMany(initialData);
            }

            _collectionInitialized = true;
        }

        private string GetCollectionName()
        {
            var currentType = GetType();
            return currentType.DeclaringType?.Name;
        }
    }
}
