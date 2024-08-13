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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class UpdateOpcodeOperation : IWriteOperation<WriteConcernResult>, IExecutableInRetryableWriteContext<WriteConcernResult>
    {
        private bool? _bypassDocumentValidation;
        private readonly CollectionNamespace _collectionNamespace;
        private int? _maxDocumentSize;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly UpdateRequest _request;
        private bool _retryRequested;
        private WriteConcern _writeConcern = WriteConcern.Acknowledged;

        public UpdateOpcodeOperation(
            CollectionNamespace collectionNamespace,
            UpdateRequest request,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _request = Ensure.IsNotNull(request, nameof(request));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
        }

        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public int? MaxDocumentSize
        {
            get { return _maxDocumentSize; }
            set { _maxDocumentSize = Ensure.IsNullOrGreaterThanZero(value, nameof(value)); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }
 
        public UpdateRequest Request
        {
            get { return _request; }
        }

        public bool RetryRequested
        {
            get { return _retryRequested; }
            set { _retryRequested = value; }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, nameof(value)); }
        }

        public WriteConcernResult Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = RetryableWriteContext.Create(binding, false, cancellationToken))
            {
                return Execute(context, cancellationToken);
            }
        }

        public WriteConcernResult Execute(RetryableWriteContext context, CancellationToken cancellationToken)
        {
            using (EventContext.BeginOperation())
            {
                var emulator = CreateEmulator();
                return emulator.Execute(context, cancellationToken);
            }
        }

        public async Task<WriteConcernResult> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (var context = await RetryableWriteContext.CreateAsync(binding, false, cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<WriteConcernResult> ExecuteAsync(RetryableWriteContext context, CancellationToken cancellationToken)
        {
            using (EventContext.BeginOperation())
            {
                var emulator = CreateEmulator();
                return await emulator.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        private UpdateOpcodeOperationEmulator CreateEmulator()
        {
            return new UpdateOpcodeOperationEmulator(_collectionNamespace, _request, _messageEncoderSettings)
            {
                BypassDocumentValidation = _bypassDocumentValidation,
                MaxDocumentSize = _maxDocumentSize,
                RetryRequested = _retryRequested,
                WriteConcern = _writeConcern
            };
        }
    }
}
