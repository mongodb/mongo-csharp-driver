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
    public sealed class UnifiedRenameCollectionOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoDatabase _database;
        private readonly string _newName;
        private readonly string _oldName;
        private readonly RenameCollectionOptions _options;

        public UnifiedRenameCollectionOperation(IMongoDatabase database, string oldName, string newName, RenameCollectionOptions options)
        {
            _database = database;
            _newName = newName;
            _oldName = oldName;
            _options = options;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _database.RenameCollection(_oldName, _newName, _options, cancellationToken);

                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _database.RenameCollectionAsync(_oldName, _newName, _options, cancellationToken);

                return OperationResult.Empty();
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedRenameCollectionOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedRenameCollectionOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedRenameCollectionOperation Build(string targetCollectionId, BsonDocument arguments)
        {
            var collection = _entityMap.Collections[targetCollectionId];
            var database = collection.Database;
            var oldName = collection.CollectionNamespace.CollectionName;
            string newName = null;
            RenameCollectionOptions options = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "dropTarget":
                        options ??= new RenameCollectionOptions();
                        options.DropTarget = argument.Value.AsNullableBoolean;
                        break;
                    case "to":
                        newName = argument.Value.AsString;
                        break;
                    default:
                        throw new FormatException($"Invalid RenameCollectionOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedRenameCollectionOperation(database, oldName, newName, options);
        }
    }
}
