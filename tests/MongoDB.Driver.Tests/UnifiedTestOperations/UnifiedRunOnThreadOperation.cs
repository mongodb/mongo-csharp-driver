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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public sealed class UnifiedRunOnThreadOperation : IUnifiedOperationWithCreateAndRunOperationCallback
    {
        private readonly Dictionary<string, Task> _threads;
        private readonly BsonDocument _operation;
        private readonly string _threadKey;

        public UnifiedRunOnThreadOperation(Dictionary<string, Task> threads, string threadKey, BsonDocument operation)
        {
            _operation = Ensure.IsNotNull(operation, nameof(operation));
            _threads = Ensure.IsNotNull(threads, nameof(threads));
            _threadKey = Ensure.IsNotNull(threadKey, nameof(threadKey));
        }

        public void Execute(Action<BsonDocument, bool, CancellationToken> createAndRunOperationCallback, CancellationToken cancellationToken)
        {
            AssignTask(createAndRunOperationCallback, async: false, cancellationToken);
        }

        public Task ExecuteAsync(Action<BsonDocument, bool, CancellationToken> createAndRunOperationCallback, CancellationToken cancellationToken)
        {
            AssignTask(createAndRunOperationCallback, async: true, cancellationToken);
            return Task.CompletedTask;
        }

        // private methods
        private void AssignTask(Action<BsonDocument, bool, CancellationToken> action, bool async, CancellationToken cancellationToken)
        {
            _threads[_threadKey] = TasksUtils.CreateTaskOnOwnThread(() => action(_operation, async, cancellationToken), cancellationToken);
        }
    }

    public sealed class UnifiedRunOnThreadOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedRunOnThreadOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedRunOnThreadOperation Build(BsonDocument arguments)
        {
            BsonDocument operation = null;
            string threadKey = null;
            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "thread":
                        threadKey = argument.Value.AsString;
                        Ensure.That(_entityMap.Threads.ContainsKey(threadKey), $"Unexpected thread: {threadKey}.");
                        break;
                    case "operation":
                        operation = argument.Value.AsBsonDocument;
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedRunOnThreadOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedRunOnThreadOperation(_entityMap.Threads, threadKey, operation);
        }
    }
}
