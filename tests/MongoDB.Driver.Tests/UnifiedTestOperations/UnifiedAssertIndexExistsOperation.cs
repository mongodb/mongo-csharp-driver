/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAssertIndexExistsOperation : IUnifiedSpecialTestOperation
    {
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly string _indexName;

        public UnifiedAssertIndexExistsOperation(
            string databaseName,
            string collectionName,
            string indexName)
        {
            _databaseName = databaseName;
            _collectionName = collectionName;
            _indexName = indexName;
        }

        public void Execute()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName);
            var collection = database.GetCollection<BsonDocument>(_collectionName);
            var indexes = collection.Indexes.List().ToList();
            var indexNames = indexes.Select(i => i["name"].AsString);

            indexNames.Should().Contain(_indexName);
        }
    }

    public class UnifiedAssertIndexExistsOperationBuilder
    {
        public UnifiedAssertIndexExistsOperation Build(BsonDocument arguments)
        {
            string collectionName = null;
            string databaseName = null;
            string indexName = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "collectionName":
                        collectionName = argument.Value.AsString;
                        break;
                    case "databaseName":
                        databaseName = argument.Value.AsString;
                        break;
                    case "indexName":
                        indexName = argument.Value.AsString;
                        break;
                    default:
                        throw new FormatException($"Invalid AssertIndexExistsOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedAssertIndexExistsOperation(databaseName, collectionName, indexName);
        }
    }
}
