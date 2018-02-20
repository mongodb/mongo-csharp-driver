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

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal class CommandWireProtocol<TCommandResult> : IWireProtocol<TCommandResult>
    {
        // private fields
        private readonly BsonDocument _additionalOptions;
        private readonly BsonDocument _command;
        private readonly Func<CommandResponseHandling> _responseHandling;
        private readonly IElementNameValidator _commandValidator;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly ReadPreference _readPreference;
        private readonly IBsonSerializer<TCommandResult> _resultSerializer;
        private readonly ICoreSession _session;

        // constructors
        public CommandWireProtocol(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            bool slaveOk,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
            : this(
                NoCoreSession.Instance,
                slaveOk ? ReadPreference.PrimaryPreferred : ReadPreference.Primary,
                databaseNamespace,
                command,
                NoOpElementNameValidator.Instance,
                null, // additionalOptions
                () => CommandResponseHandling.Return,
                resultSerializer,
                messageEncoderSettings)
        {
        }

        public CommandWireProtocol(
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            Func<CommandResponseHandling> responseHandling,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _session = Ensure.IsNotNull(session, nameof(session));
            _readPreference = readPreference;
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _command = Ensure.IsNotNull(command, nameof(command));
            _commandValidator = Ensure.IsNotNull(commandValidator, nameof(commandValidator));
            _additionalOptions = additionalOptions; // can be null
            _responseHandling = responseHandling;
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = messageEncoderSettings;
        }

        // public methods
        public TCommandResult Execute(IConnection connection, CancellationToken cancellationToken)
        {
            var supportedProtocol = CreateSupportedWireProtocol(connection);
            return supportedProtocol.Execute(connection, cancellationToken);
        }

        public Task<TCommandResult> ExecuteAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var supportedProtocol = CreateSupportedWireProtocol(connection);
            return supportedProtocol.ExecuteAsync(connection, cancellationToken);
        }

        // private methods
        private IWireProtocol<TCommandResult> CreateCommandUsingCommandMessageWireProtocol()
        {
            return new CommandUsingCommandMessageWireProtocol<TCommandResult>(
                _session,
                _readPreference,
                _databaseNamespace,
                _command,
                _commandValidator,
                _additionalOptions,
                _responseHandling,
                _resultSerializer,
                _messageEncoderSettings);
        }

        private IWireProtocol<TCommandResult> CreateCommandUsingQueryMessageWireProtocol()
        {
            return new CommandUsingQueryMessageWireProtocol<TCommandResult>(
                _session,
                _readPreference,
                _databaseNamespace,
                _command,
                _commandValidator,
                _additionalOptions,
                _responseHandling,
                _resultSerializer,
                _messageEncoderSettings);
        }

        private IWireProtocol<TCommandResult> CreateSupportedWireProtocol(IConnection connection)
        {
            var serverVersion = connection.Description?.ServerVersion;
            if (serverVersion != null && Feature.CommandMessage.IsSupported(serverVersion))
            {
                return CreateCommandUsingCommandMessageWireProtocol();
            }
            else
            {
                return CreateCommandUsingQueryMessageWireProtocol();
            }
        }
    }
}
