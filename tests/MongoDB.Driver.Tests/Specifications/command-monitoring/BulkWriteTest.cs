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
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.command_monitoring
{
    public class BulkWriteTest : CrudOperationTestBase
    {
        private List<WriteModel<BsonDocument>> _requests;
        private BulkWriteOptions _options = new BulkWriteOptions();

        protected override void Execute(IMongoCollection<BsonDocument> collection, bool async)
        {
            if (collection.Settings.WriteConcern == null)
            {
                collection = collection.WithWriteConcern(WriteConcern.Acknowledged);
            }

            if (async)
            {
                collection.BulkWriteAsync(_requests, _options).GetAwaiter().GetResult();
            }
            else
            {
                collection.BulkWrite(_requests, _options);
            }
        }

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "requests":
                    _requests = ParseRequests(value.AsBsonArray);
                    return true;
                case "options":
                    _options = ParseOptions(value.AsBsonDocument);
                    return true;
            }

            return false;
        }

        // private methods
        private BulkWriteOptions ParseOptions(BsonDocument value)
        {
            var options = new BulkWriteOptions();

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

        private List<WriteModel<BsonDocument>> ParseRequests(BsonArray requests)
        {
            var result = new List<WriteModel<BsonDocument>>();
            foreach (BsonDocument request in requests)
            {
                var name = request["name"].AsString;
                var arguments = request["arguments"].AsBsonDocument;
                switch (name)
                {
                    case "deleteOne":
                        result.Add(ParseDeleteOne(arguments));
                        break;
                    case "insertOne":
                        result.Add(ParseInsertOne(arguments));
                        break;
                    case "updateOne":
                        result.Add(ParseUpdateOne(arguments));
                        break;
                }
            }

            return result;
        }

        private DeleteOneModel<BsonDocument> ParseDeleteOne(BsonDocument request)
        {
            var filter = new BsonDocumentFilterDefinition<BsonDocument>((BsonDocument)request["filter"]);
            return new DeleteOneModel<BsonDocument>(filter);
        }

        private InsertOneModel<BsonDocument> ParseInsertOne(BsonDocument request)
        {
            return new InsertOneModel<BsonDocument>((BsonDocument)request["document"]);
        }

        private UpdateOneModel<BsonDocument> ParseUpdateOne(BsonDocument request)
        {
            var filter = new BsonDocumentFilterDefinition<BsonDocument>((BsonDocument)request["filter"]);
            var update = new BsonDocumentUpdateDefinition<BsonDocument>((BsonDocument)request["update"]);
            var model = new UpdateOneModel<BsonDocument>(filter, update);
            model.IsUpsert = request.GetValue("upsert", false).ToBoolean();
            return model;
        }
    }
}
