/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents the base class for a command operation.
    /// </summary>
    public abstract class CommandOperationBase<TCommandResult> : QueryOperationBase
    {
        // fields
        private BsonDocument _additionalOptions;
        private BsonDocument _command;
        private IElementNameValidator _commandValidator = NoOpElementNameValidator.Instance;
        private string _comment;
        private DatabaseNamespace _databaseNamespace;
        private MessageEncoderSettings _messageEncoderSettings;
        private IBsonSerializer<TCommandResult> _resultSerializer;

        // constructors
        protected CommandOperationBase(
            DatabaseNamespace databaseNamespace,
            BsonDocument command,
            IBsonSerializer<TCommandResult> resultSerializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _command = Ensure.IsNotNull(command, "command");
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public BsonDocument AdditionalOptions
        {
            get { return _additionalOptions; }
            set { _additionalOptions = value; }
        }

        public BsonDocument Command
        {
            get { return _command; }
            set { _command = Ensure.IsNotNull(value, "value"); }
        }

        public IElementNameValidator CommandValidator
        {
            get { return _commandValidator; }
            set { _commandValidator = Ensure.IsNotNull(value, "value"); }
        }

        public string Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
            set { _databaseNamespace = Ensure.IsNotNull(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        public IBsonSerializer<TCommandResult> ResultSerializer
        {
            get { return _resultSerializer; }
            set { _resultSerializer = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        private CommandWireProtocol<TCommandResult> CreateProtocol(ServerDescription serverDescription, ReadPreference readPreference)
        {
            var wrappedCommand = CreateWrappedCommand(serverDescription, readPreference);
            var slaveOk = readPreference != null && readPreference.ReadPreferenceMode != ReadPreferenceMode.Primary;
            return new CommandWireProtocol<TCommandResult>(_databaseNamespace, wrappedCommand, _commandValidator, slaveOk, _resultSerializer, _messageEncoderSettings);
        }

        private BsonDocument CreateWrappedCommand(ServerDescription serverDescription, ReadPreference readPreference)
        {
            BsonDocument readPreferenceDocument = null;
            if (serverDescription.Type == ServerType.ShardRouter)
            {
                readPreferenceDocument = CreateReadPreferenceDocument(readPreference);
            }

            var wrappedCommand = new BsonDocument
            {
                { "$query", _command },
                { "$readPreference", readPreferenceDocument, readPreferenceDocument != null },
                { "$comment", () => _comment, _comment != null }
            };
            if (_additionalOptions != null)
            {
                wrappedCommand.AddRange(_additionalOptions);
            }

            return wrappedCommand;
        }

        protected async Task<TCommandResult> ExecuteCommandAsync(
            IConnectionSource connectionSource,
            ReadPreference readPreference,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            using (var connection = await connectionSource.GetConnectionAsync(slidingTimeout, cancellationToken))
            {
                var protocol = CreateProtocol(connectionSource.ServerDescription, readPreference);
                return await protocol.ExecuteAsync(connection, slidingTimeout, cancellationToken);
            }
        }
    }
}
