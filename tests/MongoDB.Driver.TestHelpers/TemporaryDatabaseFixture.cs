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
using System.Collections.Concurrent;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver.Tests
{
    public class TemporaryDatabaseFixture: IDisposable
    {
        private static readonly string __timeStamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        protected readonly string _databaseName;
        private readonly ConcurrentBag<string> _createdCollections = new();

        protected TemporaryDatabaseFixture()
        {
            _databaseName = $"CsharpDriver-{__timeStamp}";
        }

        public virtual void Dispose()
        {
            var database = MongoDbClient.GetDatabase(_databaseName);
            foreach (var collection in _createdCollections)
            {
                database.DropCollection(collection);
            }
        }

        public IMongoClient MongoDbClient => DriverTestConfiguration.Client;

        public IMongoClient GetClient(LinqProvider provider)
            => DriverTestConfiguration.GetLinqClient(provider);

        public IMongoDatabase GetDatabase(LinqProvider provider = LinqProvider.V3)
            => GetClient(provider)
                .GetDatabase(_databaseName);

        public IMongoCollection<T> GetCollection<T>(string collectionName, LinqProvider provider = LinqProvider.V3)
        {
            _createdCollections.Add(collectionName);
            return GetDatabase(provider)
                .GetCollection<T>(collectionName);
        }
    }
}
