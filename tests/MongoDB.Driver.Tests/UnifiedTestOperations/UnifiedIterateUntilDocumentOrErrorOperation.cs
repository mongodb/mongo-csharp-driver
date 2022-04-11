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
    public class UnifiedIterateUntilDocumentOrErrorOperation<TDocument> : IUnifiedEntityTestOperation
    {
        private readonly UnifiedIterateUntilDocumentOrErrorOperationResultConverter _converter;
        private readonly IEnumerator<TDocument> _enumerator;

        public UnifiedIterateUntilDocumentOrErrorOperation(IEnumerator<TDocument> enumerator)
        {
            _converter = new UnifiedIterateUntilDocumentOrErrorOperationResultConverter();
            _enumerator = enumerator;
        }

        public OperationResult Execute(CancellationToken cancellationToken)
        {
            try
            {
                var hasNext = _enumerator.MoveNext();
                if (hasNext == false)
                {
                    throw new InvalidOperationException("Unexpected false return value from MoveNext.");
                }
                return OperationResult.FromResult(_converter.Convert(_enumerator.Current));
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
                var hasNext = _enumerator.MoveNext(); // TODO: Change to async counterpart when async enumeration is implemented
                if (hasNext == false)
                {
                    throw new InvalidOperationException("Unexpected false return value from MoveNext.");
                }
                return Task.FromResult(OperationResult.FromResult(_converter.Convert(_enumerator.Current)));
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

        public IUnifiedEntityTestOperation Build(string targetEnumeratorId, BsonDocument arguments)
        {
            if (arguments != null)
            {
                throw new FormatException("IterateUntilDocumentOrErrorOperation is not expected to contain arguments.");
            }

            if (_entityMap.ChangeStreams.TryGetValue(targetEnumeratorId, out var changeStreamEnumerator))
            {
                return new UnifiedIterateUntilDocumentOrErrorOperation<ChangeStreamDocument<BsonDocument>>(changeStreamEnumerator);
            }
            else if (_entityMap.Cursors.TryGetValue(targetEnumeratorId, out var enumerator))
            {
                return new UnifiedIterateUntilDocumentOrErrorOperation<BsonDocument>(enumerator);
            }
            else
            {
                throw new FormatException("No supported enumerator found.");
            }
        }
    }

    public class UnifiedIterateUntilDocumentOrErrorOperationResultConverter
    {
        public BsonDocument Convert<T>(T value) =>
            value switch
            {
                ChangeStreamDocument<BsonDocument> changeStreamResult => changeStreamResult.BackingDocument,
                BsonDocument bsonDocument => bsonDocument,
                _ => throw new FormatException($"Unsupported enumerator document {value.GetType().Name}.")
            };
    }
}
