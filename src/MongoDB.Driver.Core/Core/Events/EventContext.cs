/* Copyright 2015 MongoDB Inc.
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
using System.Runtime.Remoting.Messaging;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Events
{
    internal static class EventContext
    {
        private static readonly string __findBatchSize = "__MONGODB.FIND_OPERATION_BATCH_SIZE__";
        private static readonly string __findLimit = "__MONGODB.FIND_OPERATION_LIMIT__";
        private static readonly string __killCursorsNamespace = "__MONGODB.KILL_CURSORS_OPERATION_COLLECTION_NAMESPACE__";
        private static readonly string __operationId = "__MONGODB.OPERATION_ID__";

        public static int? FindOperationBatchSize
        {
            get
            {
                var value = CallContext.LogicalGetData(__findBatchSize);
                return (int?)value;
            }
            private set
            {
                CallContext.LogicalSetData(__findBatchSize, value);
            }
        }

        public static int? FindOperationLimit
        {
            get
            {
                var value = CallContext.LogicalGetData(__findLimit);
                return (int?)value;
            }
            private set
            {
                CallContext.LogicalSetData(__findLimit, value);
            }
        }

        public static CollectionNamespace KillCursorsCollectionNamespace
        {
            get
            {
                var value = CallContext.LogicalGetData(__killCursorsNamespace);
                return (CollectionNamespace)value;
            }
            private set
            {
                CallContext.LogicalSetData(__killCursorsNamespace, value);
            }
        }

        public static long? OperationId
        {
            get
            {
                var value = CallContext.LogicalGetData(__operationId);
                return (long?)value;
            }
            private set
            {
                CallContext.LogicalSetData(__operationId, value);
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
            return BeginOperation(null);
        }

        public static IDisposable BeginOperation(long? operationId)
        {
            return OperationId == null ?
                (IDisposable)new OperationIdDisposer(operationId ?? LongIdGenerator<OperationIdDisposer>.GetNextId()) :
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
            public OperationIdDisposer(long operationId)
            {
                EventContext.OperationId = operationId;
            }

            public void Dispose()
            {
                EventContext.OperationId = null;
            }
        }

    }
}
