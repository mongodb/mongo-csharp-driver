/* Copyright 2010-2014 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class ExplainOperation : IReadOperation<BsonDocument>, IWriteOperation<BsonDocument>
    {
        // fields
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly BsonDocument _command;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private ExplainVerbosity _verbosity;

        // constructors
        public ExplainOperation(DatabaseNamespace databaseNamespace, BsonDocument command, MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _command = Ensure.IsNotNull(command, "command");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
            _verbosity = ExplainVerbosity.QueryPlanner;
        }

        // properties
        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public BsonDocument Command
        {
            get { return _command; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public ExplainVerbosity Verbosity
        {
            get { return _verbosity; }
            set { _verbosity = value; }
        }

        public Task<BsonDocument> ExecuteAsync(IReadBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var command = CreateCommand();

            var operation = new ReadCommandOperation<BsonDocument>(
                _databaseNamespace,
                command,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings);

            return operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var command = CreateCommand();

            var operation = new WriteCommandOperation<BsonDocument>(
                _databaseNamespace,
                command,
                BsonDocumentSerializer.Instance,
                _messageEncoderSettings);

            return operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        private static string ConvertVerbosityToString(ExplainVerbosity verbosity)
        {
            switch(verbosity)
            {
                case ExplainVerbosity.AllPlansExecution:
                    return "allPlansExecution";
                case ExplainVerbosity.ExecutionStats:
                    return "executionStats";
                case ExplainVerbosity.QueryPlanner:
                    return "queryPlanner";
                default:
                    var message = string.Format("Unsupported explain verbosity: {0}.", verbosity.ToString());
                    throw new InvalidOperationException(message);
            }
        }

        internal BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "explain", _command },
                { "verbosity", ConvertVerbosityToString(_verbosity) }
            };
        }
    }
}