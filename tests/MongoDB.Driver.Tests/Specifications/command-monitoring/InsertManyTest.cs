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
using System.Linq;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.command_monitoring
{
    public class InsertManyTest : CrudOperationTestBase
    {
        private IEnumerable<BsonDocument> _documents;
        private InsertManyOptions _options = new InsertManyOptions();
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "documents":
                    _documents = ((BsonArray)value).OfType<BsonDocument>();
                    return true;
                case "options":
                    _options = ParseOptions(value.AsBsonDocument);
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
                collectionWithWriteConcern.InsertManyAsync(_documents, _options).GetAwaiter().GetResult();
            }
            else
            {
                collectionWithWriteConcern.InsertMany(_documents, _options);
            }
        }

        // private methods
        private InsertManyOptions ParseOptions(BsonDocument value)
        {
            var options = new InsertManyOptions();

            foreach (var option in value.Elements)
            {
                switch (option.Name)
                {
                    case "ordered":
                        options.IsOrdered = option.Value.ToBoolean();
                        break;
                    default:
                        throw new FormatException($"Unexpected option: ${option.Name}.");
                }
            }

            return options;
        }
    }
}
