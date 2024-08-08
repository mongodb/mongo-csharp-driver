/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class CommandOperationBase<TCommandResult>
    {
        private BsonDocument _additionalOptions;
        private BsonDocument _command;
        private IElementNameValidator _commandValidator = NoOpElementNameValidator.Instance;
        private string _comment;
        private DatabaseNamespace _databaseNamespace;
        private MessageEncoderSettings _messageEncoderSettings;
        private IBsonSerializer<TCommandResult> _resultSerializer;

        protected CommandOperationBase(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _command = Ensure.IsNotNull(command, nameof(command));
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = messageEncoderSettings;
        }

        public BsonDocument AdditionalOptions
        {
            get { return _additionalOptions; }
            set { _additionalOptions = value; }
        }

        public BsonDocument Command
        {
            get { return _command; }
        }

        public IElementNameValidator CommandValidator
        {
            get { return _commandValidator; }
            set { _commandValidator = Ensure.IsNotNull(value, nameof(value)); }
        }

        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public IBsonSerializer<TCommandResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        protected TCommandResult ExecuteProtocol(IChannelHandle channel, ICoreSessionHandle session, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            var additionalOptions = GetEffectiveAdditionalOptions();

            return channel.Command(
                session,
                readPreference,
                _databaseNamespace,
                _command,
                null, // commandPayloads
                _commandValidator,
                additionalOptions,
                null, // postWriteAction,
                CommandResponseHandling.Return,
                _resultSerializer,
                _messageEncoderSettings,
                cancellationToken);
        }

        protected TCommandResult ExecuteProtocol(
            IChannelSource channelSource,
            ICoreSessionHandle session,
            ReadPreference readPreference,
            CancellationToken cancellationToken)
        {
            using (var channel = channelSource.GetChannel(cancellationToken))
            {
                return ExecuteProtocol(channel, session, readPreference, cancellationToken);
            }
        }

        protected Task<TCommandResult> ExecuteProtocolAsync(IChannelHandle channel, ICoreSessionHandle session, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            var additionalOptions = GetEffectiveAdditionalOptions();

            return channel.CommandAsync(
                session,
                readPreference,
                _databaseNamespace,
                _command,
                null, // TODO: support commandPayloads
                _commandValidator,
                additionalOptions,
                null, // postWriteAction,
                CommandResponseHandling.Return,
                _resultSerializer,
                _messageEncoderSettings,
                cancellationToken);
        }

        protected async Task<TCommandResult> ExecuteProtocolAsync(
            IChannelSource channelSource,
            ICoreSessionHandle session,
            ReadPreference readPreference,
            CancellationToken cancellationToken)
        {
            using (var channel = await channelSource.GetChannelAsync(cancellationToken).ConfigureAwait(false))
            {
                return await ExecuteProtocolAsync(channel, session, readPreference, cancellationToken).ConfigureAwait(false);
            }
        }

        private BsonDocument GetEffectiveAdditionalOptions()
        {
            if (_additionalOptions == null && _comment == null)
            {
                return null;
            }
            else if (_additionalOptions != null && _comment == null)
            {
                return _additionalOptions;
            }
            else if (_additionalOptions == null && _comment != null)
            {
                return new BsonDocument("$comment", _comment);
            }
            else
            {
                var additionalOptions = new BsonDocument("$comment", _comment);
                additionalOptions.Merge(_additionalOptions, overwriteExistingElements: false);
                return additionalOptions;
            }
        }
    }
}
