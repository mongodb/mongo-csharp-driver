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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedCreateEntitiesOperation : IUnifiedSpecialTestOperation
    {
        private readonly BsonArray _entities;
        private UnifiedEntityMap _entityMap;

        public UnifiedCreateEntitiesOperation(UnifiedEntityMap entityMap, BsonArray entities)
        {
            _entities = Ensure.IsNotNull(entities, nameof(entities));
            _entityMap = Ensure.IsNotNull(entityMap, nameof(entityMap));
        }

        public void Execute()
        {
            _entityMap.AddRange(_entities);
        }
    }

    public sealed class UnifiedCreateEntitiesOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateEntitiesOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateEntitiesOperation Build(BsonDocument arguments)
        {
            BsonArray entities = null;
            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "entities":
                        entities = argument.Value.AsBsonArray;
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedCreateEntitiesOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedCreateEntitiesOperation(_entityMap, entities);
        }
    }
}
