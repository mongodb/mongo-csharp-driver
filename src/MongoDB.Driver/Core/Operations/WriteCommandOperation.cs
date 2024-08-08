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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class WriteCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IWriteOperation<TCommandResult>
    {
        private ReadPreference _readPreference = ReadPreference.Primary;

        public WriteCommandOperation(DatabaseNamespace databaseNamespace, BsonDocument command, IBsonSerializer<TCommandResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
            : base(databaseNamespace, command, resultSerializer, messageEncoderSettings)
        {
        }

        public ReadPreference ReadPreference
        {
            get => _readPreference;
            set => _readPreference = Ensure.IsNotNull(value, nameof(value));
        }

        public TCommandResult Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (EventContext.BeginOperation())
            using (var channelSource = binding.GetWriteChannelSource(cancellationToken))
            {
                return ExecuteProtocol(channelSource, binding.Session, _readPreference, cancellationToken);
            }
        }

        public async Task<TCommandResult> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (EventContext.BeginOperation())
            using (var channelSource = await binding.GetWriteChannelSourceAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteProtocolAsync(channelSource, binding.Session, _readPreference, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
