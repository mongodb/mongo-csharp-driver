/* Copyright 2018-present MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public abstract class JsonDrivenCollectionTest : JsonDrivenDatabaseTest
    {
        // protected fields
        protected IMongoCollection<BsonDocument> _collection;

        // protected constructors
        protected JsonDrivenCollectionTest(IMongoClient client, IMongoDatabase database, IMongoCollection<BsonDocument> collection, Dictionary<string, IClientSessionHandle> sessionMap)
            : base(client, database, sessionMap)
        {
            _collection = collection;
        }

        // public properties
        public IMongoCollection<BsonDocument> Collection => _collection;

        // protected methods
        protected override void SetReadPreference(ReadPreference value)
        {
            base.SetReadPreference(value);
            _collection = _collection.WithReadPreference(value);
        }

        protected override void SetWriteConcern(WriteConcern value)
        {
            base.SetWriteConcern(value);
            _collection = _collection.WithWriteConcern(value);
        }
    }
}
