/* Copyright 2015-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Events
{
    internal static class EventContext
    {
        private static readonly AsyncLocal<int?> __findOperationBatchSize = new AsyncLocal<int?>();
        private static readonly AsyncLocal<int?> __findOperationLimit = new AsyncLocal<int?>();
        private static readonly AsyncLocal<CollectionNamespace> __killCursorsCollectionNamespace = new AsyncLocal<CollectionNamespace>();
        private static readonly AsyncLocal<long?> __operationId = new AsyncLocal<long?>();
        private static readonly AsyncLocal<string> __operationName = new AsyncLocal<string>();

        public static int? FindOperationBatchSize
        {
            get
            {
                return __findOperationBatchSize.Value;
            }
            private set
            {
                __findOperationBatchSize.Value = value;
            }
        }

        public static int? FindOperationLimit
        {
            get
            {
                return __findOperationLimit.Value;
            }
            private set
            {
                __findOperationLimit.Value = value;
            }
        }

        public static CollectionNamespace KillCursorsCollectionNamespace
        {
            get
            {
                return __killCursorsCollectionNamespace.Value;
            }
            private set
            {
                __killCursorsCollectionNamespace.Value = value;
            }
        }

        public static long? OperationId
        {
            get
            {
                return __operationId.Value;
            }
            private set
            {
                __operationId.Value = value;
            }
        }

        public static string OperationName
        {
            get
            {
                return __operationName.Value;
            }
            private set
            {
                __operationName.Value = value;
            }
        }

        public static IDisposable BeginFind(int? batchSize, int? limit)
        {
            return FindOperationBatchSize == null ?
                (IDisposable)new FindOperationDisposer(batchSize, limit) :
                NoOpDisposer.Instance;
        }

        public static IDisposable BeginKillCursors(CollectionNamespace collectionNamespace)
        {
            return KillCursorsCollectionNamespace == null ?
                (IDisposable)new KillCursorsOperationDisposer(collectionNamespace) :
                NoOpDisposer.Instance;
        }

        public static IDisposable BeginOperation()
        {
            return BeginOperation(null, null);
        }

        public static IDisposable BeginOperation(string commandName)
        {
            return commandName != null ? new OperationNameDisposer(commandName) : NoOpDisposer.Instance;
        }

        public static IDisposable BeginOperation(long? operationId, string commandName = null)
        {
            return OperationId == null ?
                new OperationIdDisposer(operationId ?? LongIdGenerator<OperationIdDisposer>.GetNextId(), commandName) :
                NoOpDisposer.Instance;
        }

        private sealed class NoOpDisposer : IDisposable
        {
            public static NoOpDisposer Instance = new NoOpDisposer();

            public void Dispose()
            {
                // do nothing
            }
        }

        private sealed class FindOperationDisposer : IDisposable
        {
            public FindOperationDisposer(int? batchSize, int? limit)
            {
                EventContext.FindOperationBatchSize = batchSize;
                EventContext.FindOperationLimit = limit;
            }

            public void Dispose()
            {
                EventContext.FindOperationBatchSize = null;
                EventContext.FindOperationLimit = null;
            }
        }

        private sealed class KillCursorsOperationDisposer : IDisposable
        {
            public KillCursorsOperationDisposer(CollectionNamespace collectionNamespace)
            {
                EventContext.KillCursorsCollectionNamespace = collectionNamespace;
            }

            public void Dispose()
            {
                EventContext.KillCursorsCollectionNamespace = null;
            }
        }

        private sealed class OperationIdDisposer : IDisposable
        {
            public OperationIdDisposer(long operationId, string operationName)
            {
                EventContext.OperationId = operationId;
                EventContext.OperationName = operationName;
            }

            public void Dispose()
            {
                EventContext.OperationId = null;
                EventContext.OperationName = null;
            }
        }

        private sealed class OperationNameDisposer : IDisposable
        {
            public OperationNameDisposer(string operationName)
            {
                EventContext.OperationName = operationName;
            }

            public void Dispose()
            {
                EventContext.OperationName = null;
            }
        }
    }
}
