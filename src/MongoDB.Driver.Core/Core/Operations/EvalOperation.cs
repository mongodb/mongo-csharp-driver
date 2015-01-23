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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class EvalOperation : IWriteOperation<BsonValue>
    {
        // fields
        private IEnumerable<BsonValue> _args;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly BsonJavaScript _function;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool? _noLock;

        // constructors
        public EvalOperation(
            DatabaseNamespace databaseNamespace,
            BsonJavaScript function,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _function = Ensure.IsNotNull(function, "function");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public IEnumerable<BsonValue> Args
        {
            get { return _args; }
            set { _args = value; }
        }

        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public BsonJavaScript Function
        {
            get { return _function; }
        }

        /// <summary>
        /// Gets or sets the maximum time the server should spend on this operation.
        /// </summary>
        /// <value>
        /// The maximum time the server should spend on this operation.
        /// </value>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, "value"); }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public bool? NoLock
        {
            get { return _noLock; }
            set { _noLock = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "$eval", _function },
                { "args", () => new BsonArray(_args), _args != null },
                { "nolock", () => _noLock.Value, _noLock.HasValue },
                { "maxTimeMS", () => _maxTime.Value.TotalMilliseconds, _maxTime.HasValue }
            };
        }

        public async Task<BsonValue> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            var result = await operation.ExecuteAsync(binding, cancellationToken).ConfigureAwait(false);
            return result["retval"];
        }
    }
}
