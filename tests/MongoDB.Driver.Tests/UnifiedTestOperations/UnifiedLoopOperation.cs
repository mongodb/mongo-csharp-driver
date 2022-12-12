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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedLoopOperation : IUnifiedOperationWithCreateAndRunOperationCallback
    {
        private readonly UnifiedEntityMap _entityMap;
        private readonly BsonArray _errorDescriptionDocuments;
        private readonly BsonArray _failureDescriptionDocuments;
        private readonly BsonArray _loopOperations;
        private readonly string _storeErrorsAsEntity;
        private readonly string _storeFailuresAsEntity;
        private readonly string _storeIterationsAsEntity;
        private readonly string _storeSuccessesAsEntity;
        private readonly CancellationToken _loopCancellationToken;

        public UnifiedLoopOperation(
            UnifiedEntityMap entityMap,
            BsonArray loopOperations,
            string storeErrorsAsEntity,
            string storeFailuresAsEntity,
            string storeIterationsAsEntity,
            string storeSuccessesAsEntity,
            CancellationToken loopCancellationToken)
        {
            _entityMap = Ensure.IsNotNull(entityMap, nameof(entityMap));
            _errorDescriptionDocuments = new BsonArray();
            _failureDescriptionDocuments = new BsonArray();
            _loopOperations = Ensure.IsNotNull(loopOperations, nameof(loopOperations));
            _storeErrorsAsEntity = storeErrorsAsEntity; // can be null
            _storeFailuresAsEntity = storeFailuresAsEntity; // can be null
            _storeIterationsAsEntity = storeIterationsAsEntity; // can be null
            _storeSuccessesAsEntity = storeSuccessesAsEntity; // can be null
            _loopCancellationToken = loopCancellationToken;
        }

        public void Execute(Action<BsonDocument, bool, CancellationToken> createAndRunOperationCallback, CancellationToken cancellationToken)
        {
            Execute(createAndRunOperationCallback, async: false, cancellationToken);
        }

        public Task ExecuteAsync(Action<BsonDocument, bool, CancellationToken> createAndRunOperationCallback, CancellationToken cancellationToken)
        {
            Execute(createAndRunOperationCallback, async: true, cancellationToken);
            return Task.FromResult(true);
        }

        // private methods
        private BsonDocument CreateDocumentFromException(Exception ex) =>
            new BsonDocument
            {
                { "error", ex.ToString() },
                { "time", BsonUtils.ToSecondsSinceEpoch(DateTime.UtcNow) }
            };

        private void Execute(Action<BsonDocument, bool, CancellationToken> createAndRunOperationCallback, bool async, CancellationToken cancellationToken)
        {
            int iterationCount = 0;
            int successCount = 0;
            while (!_loopCancellationToken.IsCancellationRequested)
            {
                // Use DeepClone because further operations can mutate the passed input documents
                // due to auto adding _id from the server response
                foreach (var operation in _loopOperations.Select(o => o.DeepClone().AsBsonDocument))
                {
                    try
                    {
                        createAndRunOperationCallback(operation, async, cancellationToken);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        if (!TryHandleException(ex))
                        {
                            throw;
                        }
                        break;
                    }
                }
                iterationCount++;
            }

            HandleResults(iterationCount, successCount);
        }

        private void HandleResults(long iterationCount, long successCount)
        {
            if (_storeSuccessesAsEntity != null)
            {
                _entityMap.SuccessCounts.Add(_storeSuccessesAsEntity, successCount);
            }

            if (_storeIterationsAsEntity != null)
            {
                _entityMap.IterationCounts.Add(_storeIterationsAsEntity, iterationCount);
            }

            if (_storeFailuresAsEntity != null)
            {
                _entityMap.FailureDocuments.Add(_storeFailuresAsEntity, _failureDescriptionDocuments);
            }

            if (_storeErrorsAsEntity != null)
            {
                _entityMap.ErrorDocuments.Add(_storeErrorsAsEntity, _errorDescriptionDocuments);
            }
        }

        private bool TryHandleException(Exception ex)
        {
            if (ex is AssertionException)
            {
                if (_storeFailuresAsEntity != null)
                {
                    _failureDescriptionDocuments.Add(CreateDocumentFromException(ex));
                }
                else if (_storeErrorsAsEntity != null)
                {
                    _errorDescriptionDocuments.Add(CreateDocumentFromException(ex));
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (_storeErrorsAsEntity != null)
                {
                    _errorDescriptionDocuments.Add(CreateDocumentFromException(ex));
                }
                else if (_storeFailuresAsEntity != null)
                {
                    _failureDescriptionDocuments.Add(CreateDocumentFromException(ex));
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class UnifiedLoopOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;
        private readonly CancellationToken _loopCancellationToken;

        public UnifiedLoopOperationBuilder(UnifiedEntityMap entityMap, Dictionary<string, object> additionalArgs)
        {
            _entityMap = entityMap;
            _loopCancellationToken = (CancellationToken)Ensure.IsNotNull(additionalArgs, nameof(additionalArgs))["UnifiedLoopOperationCancellationToken"];
        }

        public UnifiedLoopOperation Build(BsonDocument arguments)
        {
            BsonArray operations = null;
            string storeErrorsAsEntity = null;
            string storeFailuresAsEntity = null;
            string storeIterationsAsEntity = null;
            string storeSuccessesAsEntity = null;

            foreach (var argument in arguments)
            {
                switch (argument.Name)
                {
                    case "operations":
                        operations = argument.Value.AsBsonArray;
                        break;
                    case "storeErrorsAsEntity":
                        storeErrorsAsEntity = argument.Value.AsString;
                        break;
                    case "storeFailuresAsEntity":
                        storeFailuresAsEntity = argument.Value.AsString;
                        break;
                    case "storeIterationsAsEntity":
                        storeIterationsAsEntity = argument.Value.AsString;
                        break;
                    case "storeSuccessesAsEntity":
                        storeSuccessesAsEntity = argument.Value.AsString;
                        break;
                    default:
                        throw new FormatException($"Invalid {nameof(UnifiedLoopOperation)} argument name: '{argument.Name}'.");
                }
            }

            return new UnifiedLoopOperation(
                _entityMap,
                operations,
                storeErrorsAsEntity,
                storeFailuresAsEntity,
                storeIterationsAsEntity,
                storeSuccessesAsEntity,
                _loopCancellationToken);
        }
    }
}
