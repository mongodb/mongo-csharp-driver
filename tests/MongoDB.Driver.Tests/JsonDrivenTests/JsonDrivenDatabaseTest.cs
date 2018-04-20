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
    public abstract class JsonDrivenDatabaseTest : JsonDrivenClientTest
    {
        // protected fields
        protected IMongoDatabase _database;

        // protected constructors
        protected JsonDrivenDatabaseTest(IMongoClient client, IMongoDatabase database, Dictionary<string, IClientSessionHandle> sessionMap)
            : base(client, sessionMap)
        {
            _database = database;
        }

        // public properties
        public IMongoDatabase Database => _database;

        // protected methods
        protected override void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "readPreference":
                    SetReadPreference(ReadPreference.FromBsonDocument(value.AsBsonDocument));
                    return;

                case "writeConcern":
                    SetWriteConcern(WriteConcern.FromBsonDocument(value.AsBsonDocument));
                    return;
            }

            base.SetArgument(name, value);
        }

        protected virtual void SetReadPreference(ReadPreference value)
        {
            _database = _database.WithReadPreference(value);
        }

        protected virtual void SetWriteConcern(WriteConcern value)
        {
            _database = _database.WithWriteConcern(value);
        }
    }
}
