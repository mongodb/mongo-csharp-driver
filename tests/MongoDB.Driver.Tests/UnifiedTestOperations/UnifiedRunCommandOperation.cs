/* Copyright 2021-present MongoDB Inc.
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
    public class UnifiedRunCommandOperation : IUnifiedEntityTestOperation
    {
        private readonly IMongoDatabase _database;
        private readonly string _commandName;
        private readonly BsonDocument _command;
        private readonly ReadPreference _readPreference = null;
        private readonly IClientSessionHandle _session = null;

        public UnifiedRunCommandOperation(
            IMongoDatabase database,
            string commandName,
            BsonDocument command,
            IClientSessionHandle session,
            ReadPreference readPreference)
        {
            _database = database;
            _commandName = commandName;
            _command = command;
            _readPreference = readPreference;
            _session = session;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var result = _session == null
                    ? _database.RunCommand<BsonDocument>(_command, _readPreference, cancellationToken: cancellationToken)
                    : _database.RunCommand<BsonDocument>(_session, _command, _readPreference, cancellationToken: cancellationToken);

                return OperationResult.FromResult(result);
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
                var result = _session == null
                    ? await _database.RunCommandAsync<BsonDocument>(_command, _readPreference, cancellationToken: cancellationToken)
                    : await _database.RunCommandAsync<BsonDocument>(_session, _command, _readPreference, cancellationToken: cancellationToken);

                return OperationResult.FromResult(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }
    }

    public class UnifiedRunCommandOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedRunCommandOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedRunCommandOperation Build(string targetDatabaseId, BsonDocument arguments)
        {
            var database = _entityMap.Databases[targetDatabaseId];

            string commandName = null;
            BsonDocument command = null;
            IClientSessionHandle session = null;
            ReadPreference readPreference = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "commandName":
                        commandName = argument.Value.AsString;
                        break;
                    case "command":
                        command = argument.Value.AsBsonDocument;
                        break;
                    case "readPreference":
                        readPreference = ReadPreference.FromBsonDocument(argument.Value.AsBsonDocument);
                        break;
                    case "session":
                        session = _entityMap.Sessions[argument.Value.AsString];
                        break;
                    default:
                        throw new FormatException($"Invalid RunCommandOperation argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedRunCommandOperation(database, commandName, command, session, readPreference);
        }
    }
}
