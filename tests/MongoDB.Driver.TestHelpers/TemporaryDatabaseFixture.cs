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
using System.Threading;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;

namespace MongoDB.Driver.Tests
{
    public class TemporaryDatabaseFixture: IDisposable
    {
        private static readonly string __timeStamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        private readonly string _databaseName = $"CsharpDriver-{__timeStamp}";
        private readonly ConcurrentBag<string> _createdCollectionNames = new();
        private readonly Lazy<DisposableMongoClient> _linq2Client = new(() => CreateMongoClient(LinqProvider.V2), LazyThreadSafetyMode.ExecutionAndPublication);
        private readonly Lazy<DisposableMongoClient> _linq3Client = new(() => CreateMongoClient(LinqProvider.V3), LazyThreadSafetyMode.ExecutionAndPublication);

        public virtual void Dispose()
        {
            var database = GetDatabase();
            foreach (var collection in _createdCollectionNames)
            {
                database.DropCollection(collection);
            }

            if (_linq2Client.IsValueCreated)
            {
                _linq2Client.Value.Dispose();
            }

            if (_linq3Client.IsValueCreated)
            {
                _linq3Client.Value.Dispose();
            }
        }

        public IMongoClient GetClient(LinqProvider provider)
            => provider switch
            {
                LinqProvider.V2 => _linq2Client.Value,
                LinqProvider.V3 => _linq3Client.Value,
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            };

        public IMongoDatabase GetDatabase(LinqProvider provider = LinqProvider.V3)
            => GetClient(provider).GetDatabase(_databaseName);

        public IMongoCollection<T> GetCollection<T>(string collectionName = null, LinqProvider provider = LinqProvider.V3)
        {
            if (string.IsNullOrEmpty(collectionName))
            {
                var stack = new System.Diagnostics.StackTrace();
                var frame = stack.GetFrame(1); // skip 1 frame to get the calling method info
                var method = frame.GetMethod();
                collectionName = $"{method.DeclaringType.Name}.{method.Name}";
            }

            var db = GetDatabase(provider);
            db.DropCollection(collectionName);
            _createdCollectionNames.Add(collectionName);
            return db.GetCollection<T>(collectionName);
        }

        private static DisposableMongoClient CreateMongoClient(LinqProvider provider)
        {
            var clientSettings = DriverTestConfiguration.GetClientSettings();
            clientSettings.LinqProvider = provider;
            return DriverTestConfiguration.CreateDisposableClient(clientSettings);
        }
    }
}
