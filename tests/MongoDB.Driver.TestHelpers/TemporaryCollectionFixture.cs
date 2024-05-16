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
    public abstract class TemporaryCollectionFixture<TModel> : TemporaryDatabaseFixture
    {
        private readonly Lazy<string> _collectionName;
        private readonly Lazy<bool> _initCollection;

        protected TemporaryCollectionFixture()
        {
            _initCollection = new Lazy<bool>(InitCollection, LazyThreadSafetyMode.ExecutionAndPublication);
            _collectionName = new Lazy<string>(GetDeclaringTypeName);
        }

        protected abstract IEnumerable<TModel> GetInitialData();

        protected virtual string CollectionName => _collectionName.Value;

        public IMongoCollection<TModel> GetCollection(LinqProvider provider = LinqProvider.V3)
        {
            // should always call this to ensure the collection is being initialized with the data
            _ = _initCollection.Value;
            return GetCollection<TModel>(CollectionName, provider);
        }

        public IMongoCollection<T> GetCollection<T>(LinqProvider provider = LinqProvider.V3)
        {
            // should always call this to ensure the collection is being initialized with the data
            _ = _initCollection.Value;
            return GetCollection<T>(CollectionName, provider);
        }

        private string GetDeclaringTypeName()
        {
            var currentType = GetType();
            return currentType.DeclaringType?.Name;
        }

        private bool InitCollection()
        {
            if (string.IsNullOrEmpty(CollectionName))
            {
                throw new InvalidOperationException("Cannot resolve the collection name. Try to override the CollectionName property and return the name explicitly");
            }

            GetDatabase().DropCollection(CollectionName);
            var collection = GetCollection<TModel>(CollectionName);
            var initialData = GetInitialData();
            if (initialData != null)
            {
                collection.InsertMany(initialData);
            }

            return true;
        }
    }
}
