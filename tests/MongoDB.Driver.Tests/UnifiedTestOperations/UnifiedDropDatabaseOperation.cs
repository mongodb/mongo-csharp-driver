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

namespace MongoDB.Driver.Tests.UnifiedTestOperations;

public class UnifiedDropDatabaseOperation : IUnifiedEntityTestOperation
{
    private readonly IMongoClient _client;
    private readonly string _databaseName;
    private readonly IClientSessionHandle _session;

    public UnifiedDropDatabaseOperation(
        IMongoClient client,
        IClientSessionHandle session,
        string databaseName)
    {
        _client = client;
        _session = session;
        _databaseName = databaseName;
    }

    public OperationResult Execute(CancellationToken cancellationToken)
    {
        try
        {
            if (_session == null)
            {
                _client.DropDatabase(_databaseName, cancellationToken);
            }
            else
            {
                _client.DropDatabase(_session, _databaseName, cancellationToken);
            }

            return OperationResult.FromResult(null);
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
            if (_session == null)
            {
                await _client.DropDatabaseAsync(_databaseName, cancellationToken);
            }
            else
            {
                await _client.DropDatabaseAsync(_session, _databaseName, cancellationToken);
            }

            return OperationResult.FromResult(null);
        }
        catch (Exception exception)
        {
            return OperationResult.FromException(exception);
        }
    }
}

public class UnifiedDropDatabaseOperationBuilder
{
    private readonly UnifiedEntityMap _entityMap;

    public UnifiedDropDatabaseOperationBuilder(UnifiedEntityMap entityMap)
    {
        _entityMap = entityMap;
    }

    public UnifiedDropDatabaseOperation Build(string targetClientId, BsonDocument arguments)
    {
        var client = _entityMap.Clients[targetClientId];

        string databaseName = null;
        IClientSessionHandle session = null;

        foreach (var argument in arguments)
        {
            switch (argument.Name)
            {
                case "database":
                    databaseName = argument.Value.AsString;
                    break;
                case "session":
                    session = _entityMap.Sessions[argument.Value.AsString];
                    break;
                default:
                    throw new FormatException($"Invalid DropCollectionOperation argument name: '{argument.Name}'.");
            }
        }

        return new UnifiedDropDatabaseOperation(client, session, databaseName);
    }
}
