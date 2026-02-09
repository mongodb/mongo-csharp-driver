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

public class UnifiedGetSnapshotTimeOperation : IUnifiedEntityTestOperation
{
    private readonly IClientSessionHandle _session;

    public UnifiedGetSnapshotTimeOperation(IClientSessionHandle session)
    {
        _session = session;
    }

    public OperationResult Execute(CancellationToken cancellationToken) => GetSnapshotTime();

    public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken) => Task.FromResult(GetSnapshotTime());

    private OperationResult GetSnapshotTime()
    {
        try
        {
            return OperationResult.FromResult(_session.GetSnapshotTime());
        }
        catch (Exception exception)
        {
            return OperationResult.FromException(exception);
        }
    }
}

public class UnifiedGetSnapshotTimeOperationBuilder
{
    private readonly UnifiedEntityMap _entityMap;

    public UnifiedGetSnapshotTimeOperationBuilder(UnifiedEntityMap entityMap)
    {
        _entityMap = entityMap;
    }

    public UnifiedGetSnapshotTimeOperation Build(string targetSessionId, BsonDocument arguments)
    {
        if (arguments != null)
        {
            throw new FormatException("GetSnapshotTime is not expected to contain arguments.");
        }

        var session = _entityMap.Sessions[targetSessionId];
        return new UnifiedGetSnapshotTimeOperation(session);
    }
}