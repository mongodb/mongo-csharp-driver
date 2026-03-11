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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class EstimatedDocumentCountOperation : IReadOperation<long>
    {
        private readonly CollectionNamespace _collectionNamespace;
        private BsonValue _comment;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private ReadConcern _readConcern = ReadConcern.Default;
        private bool _retryRequested;

        public EstimatedDocumentCountOperation(CollectionNamespace collectionNamespace, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public CollectionNamespace CollectionNamespace => _collectionNamespace;

        public BsonValue Comment
        {
            get => _comment;
            set => _comment = value;
        }

        public TimeSpan? MaxTime
        {
            get => _maxTime;
            set => _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value));
        }

        public MessageEncoderSettings MessageEncoderSettings => _messageEncoderSettings;

        public string OperationName => "count";

        public ReadConcern ReadConcern
        {
            get => _readConcern;
            set => _readConcern = Ensure.IsNotNull(value, nameof(value));
        }

        public bool RetryRequested
        {
            get => _retryRequested;
            set => _retryRequested = value;
        }

        public long Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                var operation = CreateCountOperation();

                return operation.Execute(operationContext, context);
            }
        }

        public async Task<long> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var context = RetryableReadContext.Create(operationContext, binding, _retryRequested))
            {
                var operation = CreateCountOperation();

                return await operation.ExecuteAsync(operationContext, context).ConfigureAwait(false);
            }
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation(OperationName);

        private IExecutableInRetryableReadContext<long> CreateCountOperation()
        {
            var countOperation = new CountOperation(_collectionNamespace, _messageEncoderSettings)
            {
                Comment = _comment,
                MaxTime = _maxTime,
                ReadConcern = _readConcern,
                RetryRequested = _retryRequested
            };
            return countOperation;
        }
    }
}
