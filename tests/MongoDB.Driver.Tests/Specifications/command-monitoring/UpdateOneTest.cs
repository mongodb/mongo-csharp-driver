/* Copyright 2010-2015 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.command_monitoring
{
    public class UpdateOneTest : CrudOperationTestBase
    {
        private BsonDocument _filter;
        private BsonDocument _update;
        private UpdateOptions _options = new UpdateOptions();
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = (BsonDocument)value;
                    return true;
                case "update":
                    _update = (BsonDocument)value;
                    return true;
                case "upsert":
                    _options.IsUpsert = value.ToBoolean();
                    return true;
                case "writeConcern":
                    _writeConcern = WriteConcern.FromBsonDocument((BsonDocument)value);
                    return true;
            }

            return false;
        }

        protected override void Execute(IMongoCollection<BsonDocument> collection, bool async)
        {
            var collectionWithWriteConcern = collection.WithWriteConcern(_writeConcern);
            if (async)
            {
                collectionWithWriteConcern.UpdateOneAsync(_filter, _update, _options).GetAwaiter().GetResult();
            }
            else
            {
                collectionWithWriteConcern.UpdateOne(_filter, _update, _options);
            }
        }
    }
}
