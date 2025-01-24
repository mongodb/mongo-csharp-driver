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
    public abstract class MongoCollectionFixture<TDocument> : MongoDatabaseFixture
    {
        private readonly Lazy<IMongoCollection<TDocument>> _collection;
        private readonly string _collectionName;
        private bool _dataInitialized;

        protected MongoCollectionFixture(string collectionName = null)
        {
            _collectionName = collectionName ?? GetCollectionName();
            if (string.IsNullOrEmpty(_collectionName))
            {
                throw new ArgumentNullException(nameof(collectionName) , "Cannot resolve the collection name. Try to specify the parameter explicitly");
            }

            _collection = new Lazy<IMongoCollection<TDocument>>(CreateCollection);
        }

        public IMongoCollection<TDocument> Collection => _collection.Value;
        public string CollectionName => _collectionName;
        protected abstract IEnumerable<TDocument> InitialData { get; }

        public virtual bool InitializeDataBeforeEachTestCase => false;

        protected virtual IMongoCollection<TDocument> CreateCollection()
        {
            return CreateCollection<TDocument>(_collectionName);
        }

        protected override void InitializeTestCase()
        {
            if (InitializeDataBeforeEachTestCase || !_dataInitialized)
            {
                Collection.Database.DropCollection(_collectionName);
                Collection.Database.CreateCollection(_collectionName);

                var initialData = InitialData;
                if (initialData != null)
                {
                    Collection.InsertMany(initialData);
                }

                _dataInitialized = true;
            }
        }

        private string GetCollectionName()
        {
            var currentType = GetType();
            return currentType.DeclaringType?.Name;
        }
    }
}
