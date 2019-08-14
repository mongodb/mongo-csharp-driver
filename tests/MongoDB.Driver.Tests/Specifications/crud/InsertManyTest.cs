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

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class InsertManyTest : CrudOperationTestBase
    {
        private List<BsonDocument> _documents;
        private InsertManyOptions _options;

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "documents":
                    _documents = ((BsonArray)value).Select(x => (BsonDocument)x).ToList();
                    return true;
                case "options":
                    _options = ParseOptions(value.AsBsonDocument);
                    return true;
            }

            return false;
        }

        protected override void Execute(IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument outcome, bool async)
        {
            if (async)
            {
                collection.InsertManyAsync(_documents, _options).GetAwaiter().GetResult();
            }
            else
            {
                collection.InsertMany(_documents, _options);
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
                    case "bypassDocumentValidation":
                        options.BypassDocumentValidation = option.Value.ToBoolean();
                        break;
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
