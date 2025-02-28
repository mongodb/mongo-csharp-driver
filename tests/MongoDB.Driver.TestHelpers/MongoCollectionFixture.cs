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
    public abstract class MongoCollectionFixture<TDocument> : MongoCollectionFixture<TDocument, TDocument>
    {
    }

    public abstract class MongoCollectionFixture<TDocument, TInitial> : MongoDatabaseFixture
    {
        private readonly Lazy<IMongoCollection<TDocument>> _collection;
        private bool _dataInitialized;

        protected MongoCollectionFixture()
        {
            _collection = new Lazy<IMongoCollection<TDocument>>(CreateCollection);
        }

        public IMongoCollection<TDocument> Collection => _collection.Value;

        protected abstract IEnumerable<TInitial> InitialData { get; }

        public virtual bool InitializeDataBeforeEachTestCase => false;

        protected override void InitializeTestCase()
        {
            if (InitializeDataBeforeEachTestCase || !_dataInitialized)
            {
                Collection.Database.DropCollection(Collection.CollectionNamespace.CollectionName);

                if (InitialData == null)
                {
                    Collection.Database.CreateCollection(Collection.CollectionNamespace.CollectionName);
                }
                else
                {
                    var collection = Database.GetCollection<TInitial>(Collection.CollectionNamespace.CollectionName);
                    collection.InsertMany(InitialData);
                }

                _dataInitialized = true;
            }
        }

        protected virtual string GetCollectionName()
        {
            var currentType = GetType();
            var result = currentType.DeclaringType?.Name;

            if (string.IsNullOrEmpty(result))
            {
                throw new InvalidOperationException("Cannot resolve the collection name. Try to override GetCollectionName for custom collection name resolution.");
            }

            return result;
        }

        private IMongoCollection<TDocument> CreateCollection()
        {
            return CreateCollection<TDocument>(GetCollectionName());
        }
    }
}
