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

        public static FindOperationDisposer BeginFind(int? batchSize, int? limit)
        {
            return FindOperationBatchSize == null ?
                new FindOperationDisposer(batchSize, limit) :
                default;
        }

        public static KillCursorsOperationDisposer BeginKillCursors(CollectionNamespace collectionNamespace)
        {
            return KillCursorsCollectionNamespace == null ?
                new KillCursorsOperationDisposer(collectionNamespace) :
                default;
        }

        public static OperationIdDisposer BeginOperation() => BeginOperation(null, null);

        public static OperationNameDisposer BeginOperation(string commandName)
        {
            return commandName != null ? new OperationNameDisposer(commandName) : default;
        }

        public static OperationIdDisposer BeginOperation(long? operationId, string commandName = null)
        {
            return OperationId == null ?
                new OperationIdDisposer(operationId ?? LongIdGenerator<OperationIdDisposer>.GetNextId(), commandName) :
                default;
        }

        internal readonly struct FindOperationDisposer : IDisposable
        {
            private readonly bool _isActive;

            public FindOperationDisposer(int? batchSize, int? limit)
            {
                _isActive = true;
                EventContext.FindOperationBatchSize = batchSize;
                EventContext.FindOperationLimit = limit;
            }

            public readonly void Dispose()
            {
                if (_isActive)
                {
                    EventContext.FindOperationBatchSize = null;
                    EventContext.FindOperationLimit = null;
                }
            }
        }

        internal readonly struct KillCursorsOperationDisposer : IDisposable
        {
            private readonly bool _isActive;

            public KillCursorsOperationDisposer(CollectionNamespace collectionNamespace)
            {
                _isActive = true;
                EventContext.KillCursorsCollectionNamespace = collectionNamespace;
            }

            public readonly void Dispose()
            {
                if (_isActive)
                {
                    EventContext.KillCursorsCollectionNamespace = null;
                }
            }
        }

        internal readonly struct OperationIdDisposer : IDisposable
        {
            private readonly bool _isActive;

            public OperationIdDisposer(long operationId, string operationName)
            {
                _isActive = true;
                EventContext.OperationId = operationId;
                EventContext.OperationName = operationName;
            }

            public readonly void Dispose()
            {
                if (_isActive)
                {
                    EventContext.OperationId = null;
                    EventContext.OperationName = null;
                }
            }
        }

        internal readonly struct OperationNameDisposer : IDisposable
        {
            private readonly bool _isActive;

            public OperationNameDisposer(string operationName)
            {
                _isActive = true;
                EventContext.OperationName = operationName;
            }

            public readonly void Dispose()
            {
                if (_isActive)
                {
                    EventContext.OperationName = null;
                }
            }
        }
    }
}
