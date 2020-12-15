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
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedAssertCollectionNotExistsOperation : IUnifiedSpecialTestOperation
    {
        private readonly string _collectionName;
        private readonly string _databaseName;

        public UnifiedAssertCollectionNotExistsOperation(
            string databaseName,
            string collectionName)
        {
            _databaseName = databaseName;
            _collectionName = collectionName;
        }

        public void Execute()
        {
            var client = DriverTestConfiguration.Client;
            var collectionNames = client.GetDatabase(_databaseName).ListCollectionNames().ToList();

            collectionNames.Should().NotContain(_collectionName);
        }
    }

    public class UnifiedAssertCollectionNotExistsOperationBuilder
    {
        public UnifiedAssertCollectionNotExistsOperation Build(BsonDocument arguments)
        {
            string collectionName = null;
            string databaseName = null;

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
                    default:
                        throw new FormatException($"Invalid AssertCollectionNotExistsOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedAssertCollectionNotExistsOperation(databaseName, collectionName);
        }
    }
}
