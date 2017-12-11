/* Copyright 2017 MongoDB Inc.
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

namespace MongoDB.Driver.Tests.Specifications.retryable_writes
{
    public class InsertManyTest : RetryableWriteTestBase
    {
        // private fields
        private IEnumerable<BsonDocument> _documents;
        private InsertManyOptions _options;

        // public methods
        public override void Initialize(BsonDocument operation)
        {
            VerifyFields(operation, "name", "arguments");

            foreach (var argument in operation["arguments"].AsBsonDocument)
            {
                switch (argument.Name)
                {
                    case "documents":
                        _documents = argument.Value.AsBsonArray.Cast<BsonDocument>();
                        break;

                    case "options":
                        _options = ParseOptions(argument.Value.AsBsonDocument);
                        break;

                    default:
                        throw new ArgumentException($"Unexpected argument: {argument.Name}.");
                }
            }
        }

        // protected methods
        protected override void ExecuteAsync(IMongoCollection<BsonDocument> collection)
        {
            collection.InsertManyAsync(_documents, _options).GetAwaiter().GetResult();
        }

        protected override void ExecuteSync(IMongoCollection<BsonDocument> collection)
        {
            collection.InsertMany(_documents, _options);
        }

        protected override void VerifyResult(BsonDocument result)
        {
            // test specifies a result but in the .NET driver InsertMany is a void method
        }

        // private methods
        private InsertManyOptions ParseOptions(BsonDocument optionsDocument)
        {
            var options = new InsertManyOptions();

            foreach (var option in optionsDocument)
            {
                switch (option.Name)
                {
                    case "ordered":
                        options.IsOrdered = option.Value.ToBoolean();
                        break;

                    default:
                        throw new ArgumentException($"Unexpected option: {option.Name}.");
                }
            }

            return options;
        }
    }
}
