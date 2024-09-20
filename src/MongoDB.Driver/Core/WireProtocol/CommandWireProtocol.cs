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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    internal sealed class CommandWireProtocol<TCommandResult> : IWireProtocol<TCommandResult>
    {
        // private fields
        private readonly BsonDocument _additionalOptions;
        private IWireProtocol<TCommandResult> _cachedWireProtocol;
        private ConnectionId _cachedConnectionId;
        private readonly BsonDocument _command;
        private readonly List<BatchableCommandMessageSection> _commandPayloads;
        private readonly IElementNameValidator _commandValidator;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly Action<IMessageEncoderPostProcessor> _postWriteAction;
        private readonly ReadPreference _readPreference;
        private readonly CommandResponseHandling _responseHandling;
        private readonly IBsonSerializer<TCommandResult> _resultSerializer;
        private readonly ServerApi _serverApi;
        private readonly ICoreSession _session;

        // constructors
        public CommandWireProtocol(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            bool secondaryOk,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            ServerApi serverApi)
            : this(
                databaseNamespace,
                command,
                secondaryOk,
                CommandResponseHandling.Return,
                resultSerializer,
                messageEncoderSettings,
                serverApi)
        {
        }

        public CommandWireProtocol(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            bool secondaryOk,
            CommandResponseHandling commandResponseHandling,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            ServerApi serverApi)
            : this(
                NoCoreSession.Instance,
                secondaryOk ? ReadPreference.PrimaryPreferred : ReadPreference.Primary,
                databaseNamespace,
                command,
                null, // commandPayloads
                NoOpElementNameValidator.Instance,
                null, // additionalOptions
                null, // postWriteAction
                commandResponseHandling,
                resultSerializer,
                messageEncoderSettings,
                serverApi)
        {
        }

        public CommandWireProtocol(
            ICoreSession session,
            ReadPreference readPreference,
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IEnumerable<BatchableCommandMessageSection> commandPayloads,
            IElementNameValidator commandValidator,
            BsonDocument additionalOptions,
            Action<IMessageEncoderPostProcessor> postWriteAction,
            CommandResponseHandling responseHandling,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings,
            ServerApi serverApi)
        {
            if (responseHandling != CommandResponseHandling.Return &&
                responseHandling != CommandResponseHandling.NoResponseExpected &&
                responseHandling != CommandResponseHandling.ExhaustAllowed)
            {
                throw new ArgumentException("CommandResponseHandling must be Return, NoneExpected or ExhaustAllowed.", nameof(responseHandling));
            }

            _session = Ensure.IsNotNull(session, nameof(session));
            _readPreference = readPreference;
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _command = Ensure.IsNotNull(command, nameof(command));
            _commandPayloads = commandPayloads?.ToList(); // can be null
            _commandValidator = Ensure.IsNotNull(commandValidator, nameof(commandValidator));
            _additionalOptions = additionalOptions; // can be null
            _responseHandling = responseHandling;
            _resultSerializer = Ensure.IsNotNull(resultSerializer, nameof(resultSerializer));
            _messageEncoderSettings = messageEncoderSettings;
            _postWriteAction = postWriteAction; // can be null
            _serverApi = serverApi; // can be null
        }

        // public properties
        public bool MoreToCome => _cachedWireProtocol?.MoreToCome ?? false;

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
                _commandPayloads,
                _commandValidator,
                _additionalOptions,
                _responseHandling,
                _resultSerializer,
                _messageEncoderSettings,
                _postWriteAction,
                _serverApi);
        }

        private IWireProtocol<TCommandResult> CreateCommandUsingQueryMessageWireProtocol()
        {
            var responseHandling = _responseHandling == CommandResponseHandling.NoResponseExpected ? CommandResponseHandling.Ignore : _responseHandling;

            return new CommandUsingQueryMessageWireProtocol<TCommandResult>(
                _session,
                _readPreference,
                _databaseNamespace,
                _command,
                _commandPayloads,
                _commandValidator,
                _additionalOptions,
                responseHandling,
                _resultSerializer,
                _messageEncoderSettings,
                _postWriteAction,
                _serverApi);
        }

        private IWireProtocol<TCommandResult> CreateSupportedWireProtocol(IConnection connection)
        {
            if (_cachedWireProtocol != null && _cachedConnectionId == connection.ConnectionId)
            {
                // Switch to using OP_MSG if supported by server
                if (connection.Description?.MaxWireVersion >= WireVersion.Server36 && _cachedWireProtocol is CommandUsingQueryMessageWireProtocol<TCommandResult>)
                {
                    return _cachedWireProtocol = CreateCommandUsingCommandMessageWireProtocol();
                }

                return _cachedWireProtocol;
            }
            else
            {
                _cachedConnectionId = connection.ConnectionId;
                // If server API versioning has been requested, then we SHOULD send the initial hello command
                // using OP_MSG. Since this is the first message and hello/legacy hello hasn't been sent yet,
                // connection.Description will be null and we can't rely on the server check to determine if
                // the server supports OP_MSG.
                // As well since server API versioning is supported on MongoDB 5.0+, we also know that
                // OP_MSG will be supported regardless and can skip the server checks for other messages.
                if (_serverApi != null || connection.Description?.MaxWireVersion >= WireVersion.Server36 || connection.Settings.LoadBalanced)
                {
                    return _cachedWireProtocol = CreateCommandUsingCommandMessageWireProtocol();
                }
                else
                {
                    // The driver doesn't support servers less than 4.0. However it's still useful to support OP_QUERY for initial handshake.
                    // For pre-3.6 servers, it will allow throwing unsupported wire protocol exception on the driver side.
                    // If we only supported OP_MSG, we would throw a general server error about closing connection without actual reason of why it happened
                    return _cachedWireProtocol = CreateCommandUsingQueryMessageWireProtocol();
                }
            }
        }
    }
}
