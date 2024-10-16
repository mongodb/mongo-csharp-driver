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
    public class DatabaseFixture: IDisposable
    {
        private static readonly string __timeStamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        private readonly string _databaseName = $"CsharpDriver-{__timeStamp}";
        private readonly HashSet<string> _usedCollections = new();

        public virtual void Dispose()
        {
            var database = GetDatabase();
            foreach (var collection in _usedCollections)
            {
                database.DropCollection(collection);
            }
        }

        public virtual IMongoClient GetMongoClient(Action<MongoClientSettings> configure = null)
        {
            var clientSettings = DriverTestConfiguration.GetClientSettings();
            configure?.Invoke(clientSettings);
            return new MongoClient(clientSettings);
        }

        public virtual IMongoDatabase GetDatabase(Action<MongoClientSettings> configure = null)
            => GetMongoClient(configure).GetDatabase(_databaseName);

        public virtual IMongoCollection<T> GetCollection<T>(Action<MongoClientSettings> configure = null, string collectionName = null)
        {
            if (string.IsNullOrEmpty(collectionName))
            {
                var stack = new System.Diagnostics.StackTrace();
                var frame = stack.GetFrame(1); // skip 1 frame to get the calling method info
                var method = frame.GetMethod();
                collectionName = $"{method.DeclaringType.Name}.{method.Name}";
            }

            var db = GetDatabase(configure);
            db.DropCollection(collectionName);
            _usedCollections.Add(collectionName);
            return db.GetCollection<T>(collectionName);
        }
    }
}
