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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.command_monitoring
{
    public class BulkWriteTest : CrudOperationTestBase
    {
        private List<WriteModel<BsonDocument>> _requests;
        private BulkWriteOptions _options = new BulkWriteOptions();
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        protected override void Execute(IMongoCollection<BsonDocument> collection, bool async)
        {
            var collectionWithWriteConcern = collection.WithWriteConcern(_writeConcern);
            if (async)
            {
                collectionWithWriteConcern.BulkWriteAsync(_requests, _options).GetAwaiter().GetResult();
            }
            else
            {
                collectionWithWriteConcern.BulkWrite(_requests, _options);
            }
        }

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "requests":
                    _requests = ParseRequests((BsonArray)value).ToList();
                    return true;
                case "ordered":
                    _options.IsOrdered = value.ToBoolean();
                    return true;
                case "writeConcern":
                    _writeConcern = WriteConcern.FromBsonDocument((BsonDocument)value);
                    return true;
            }

            return false;
        }

        private IEnumerable<WriteModel<BsonDocument>> ParseRequests(BsonArray requests)
        {
            foreach (BsonDocument request in requests)
            {
                var element = request.GetElement(0);
                switch (element.Name)
                {
                    case "deleteOne":
                        yield return ParseDeleteOne((BsonDocument)element.Value);
                        break;
                    case "insertOne":
                        yield return ParseInsertOne((BsonDocument)element.Value);
                        break;
                    case "updateOne":
                        yield return ParseUpdateOne((BsonDocument)element.Value);
                        break;
                }
            }
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
