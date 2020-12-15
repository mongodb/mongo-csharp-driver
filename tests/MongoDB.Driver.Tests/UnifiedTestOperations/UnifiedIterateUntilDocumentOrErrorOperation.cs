/* Copyright 2020-present MongoDB Inc.
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

namespace MongoDB.Driver.Tests.UnifiedTestOperations
{
    public class UnifiedIterateUntilDocumentOrErrorOperation : IUnifiedEntityTestOperation
    {
        private readonly IEnumerator<ChangeStreamDocument<BsonDocument>> _changeStream;

        public UnifiedIterateUntilDocumentOrErrorOperation(IEnumerator<ChangeStreamDocument<BsonDocument>> changeStream)
        {
            _changeStream = changeStream;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                _changeStream.MoveNext();
                var result = _changeStream.Current;

                return new UnifiedIterateUntilDocumentOrErrorOperationResultConverter().Convert(result);
            }
            catch (Exception exception)
            {
                return OperationResult.FromException(exception);
            }
        }

        public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _changeStream.MoveNext(); // TODO: Change to async counterpart when async enumeration is implemented
                var result = _changeStream.Current;

                return Task.FromResult(new UnifiedIterateUntilDocumentOrErrorOperationResultConverter().Convert(result));
            }
            catch (Exception exception)
            {
                return Task.FromResult(OperationResult.FromException(exception));
            }
        }
    }

    public class UnifiedIterateUntilDocumentOrErrorOperationBuilder
    {
        private readonly UnifiedEntityMap _entityMap;

        public UnifiedIterateUntilDocumentOrErrorOperationBuilder(UnifiedEntityMap entityMap)
        {
            _entityMap = entityMap;
        }

        public UnifiedIterateUntilDocumentOrErrorOperation Build(string targetChangeStreamId, BsonDocument arguments)
        {
            var changeStream = _entityMap.GetChangeStream(targetChangeStreamId);

            if (arguments != null)
            {
                throw new FormatException("IterateUntilDocumentOrErrorOperation is not expected to contain arguments.");
            }

            return new UnifiedIterateUntilDocumentOrErrorOperation(changeStream);
        }
    }

    public class UnifiedIterateUntilDocumentOrErrorOperationResultConverter
    {
        public OperationResult Convert(ChangeStreamDocument<BsonDocument> result)
        {
            var document = new BsonDocument
            {
                { "operationType", result.OperationType.ToString().ToLowerInvariant() },
                { "ns", ConvertNamespace(result.CollectionNamespace) },
                { "fullDocument", result.FullDocument }
            };

            return OperationResult.FromResult(document);
        }

        private BsonValue ConvertNamespace(CollectionNamespace collectionNamespace)
        {
            return new BsonDocument
            {
                { "db", collectionNamespace.DatabaseNamespace.DatabaseName },
                { "coll", collectionNamespace.CollectionName }
            };
        }
    }
}
