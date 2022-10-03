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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedCreateEntitiesOperation : IUnifiedEntityTestOperation
    {
        private readonly BsonArray _entitiesArray;
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateEntitiesOperation(BsonArray entitiesArray, UnifiedEntityMap entityMap)
        {
            _entitiesArray = entitiesArray;
            _entityMap = entityMap;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var newEntityMap = new UnifiedEntityMapBuilder(null, _entityMap.LoggingSettings).Build(_entitiesArray);
                _entityMap.AddEntities(newEntityMap);

                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken) =>
           Task.FromResult(Execute(cancellationToken));
    }

    public class UnifiedCreateEntitiesOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedCreateEntitiesOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedCreateEntitiesOperation Build(BsonDocument arguments)
        {
            if (arguments.ElementCount > 1)
            {
                throw new FormatException($"{nameof(UnifiedCreateEntitiesOperation)} does not expected any arguments except 'entities'.");
            }

            var entities = arguments["entities"].AsBsonArray;

            return new(entities, _entityMap);
        }
    }
}
