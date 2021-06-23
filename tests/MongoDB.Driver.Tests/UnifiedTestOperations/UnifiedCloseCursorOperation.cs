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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedCloseCursorOperation<T> : IUnifiedEntityTestOperation
    {
        private readonly AsyncCursor<T> _cursor;

        public UnifiedCloseCursorOperation(AsyncCursor<T> cursor)
        {
            _cursor = Ensure.IsNotNull(cursor, nameof(cursor));
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _cursor.Close(cancellationToken);
                return OperationResult.Empty();
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }

        public async Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _cursor.CloseAsync(cancellationToken).ConfigureAwait(false);
                return OperationResult.Empty();
            }
            catch (Exception ex)
            {
                return OperationResult.FromException(ex);
            }
        }
    }

    public class UnifiedCloseCursorOperationBuilder
    {
        private UnifiedEntityMap _entityMap;

        public UnifiedCloseCursorOperationBuilder(UnifiedEntityMap entityMap) => _entityMap = entityMap;

        public IUnifiedEntityTestOperation Build(string targetEntityId, BsonDocument operationArguments)
        {
            if (_entityMap.ChangeStreams.TryGetValue(targetEntityId, out var changeStreamEnumerator))
            {
                var changeStreamCursor = (ChangeStreamCursor<ChangeStreamDocument<BsonDocument>>)GetCursor(changeStreamEnumerator);
                var asyncCursor = (AsyncCursor<RawBsonDocument>)changeStreamCursor._cursor();
                return new UnifiedCloseCursorOperation<RawBsonDocument>(asyncCursor);
            }
            else if (_entityMap.Cursors.TryGetValue(targetEntityId, out var enumerator))
            {
                var asyncCursor = (AsyncCursor<BsonDocument>)GetCursor(enumerator);
                return new UnifiedCloseCursorOperation<BsonDocument>(asyncCursor);
            }
            else
            {
                throw new FormatException("No supported enumerator found.");
            }

            IAsyncCursor<T> GetCursor<T>(IEnumerator<T> enumerator) => ((AsyncCursorEnumerator<T>)enumerator)._cursor();
        }
    }

    internal static class AsyncCursorEnumeratorReflector
    {
        public static IAsyncCursor<TDocument> _cursor<TDocument>(this AsyncCursorEnumerator<TDocument> enumerator)
        {
            return (IAsyncCursor<TDocument>)Reflector.GetFieldValue(enumerator, nameof(_cursor));
        }
    }

    internal static class ChangeStreamCursorReflector
    { 
        public static IAsyncCursor<RawBsonDocument> _cursor(this ChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor)
        {
            return (IAsyncCursor<RawBsonDocument>)Reflector.GetFieldValue(cursor, nameof(_cursor));
        }
    }
}
