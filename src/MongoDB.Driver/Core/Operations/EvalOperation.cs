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
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class EvalOperation : IWriteOperation<BsonValue>
    {
        private IEnumerable<BsonValue> _args;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly BsonJavaScript _function;
        private TimeSpan? _maxTime;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private bool? _noLock;

        public EvalOperation(
            DatabaseNamespace databaseNamespace,
            BsonJavaScript function,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _function = Ensure.IsNotNull(function, nameof(function));
            _messageEncoderSettings = messageEncoderSettings;
        }

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

        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = Ensure.IsNullOrInfiniteOrGreaterThanOrEqualToZero(value, nameof(value)); }
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

        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "$eval", _function },
                { "args", () => new BsonArray(_args), _args != null },
                { "nolock", () => _noLock.Value, _noLock.HasValue },
                { "maxTimeMS", () => MaxTimeHelper.ToMaxTimeMS(_maxTime.Value), _maxTime.HasValue }
            };
        }

        public BsonValue Execute(IWriteBinding binding, OperationContext operationContext)
        {
            Ensure.IsNotNull(binding, nameof(binding));
            var operation = CreateOperation();
            var result = operation.Execute(binding, operationContext);
            return result["retval"];
        }

        public async Task<BsonValue> ExecuteAsync(IWriteBinding binding, OperationContext operationContext)
        {
            Ensure.IsNotNull(binding, nameof(binding));
            var operation = CreateOperation();
            var result = await operation.ExecuteAsync(binding, operationContext).ConfigureAwait(false);
            return result["retval"];
        }

        private WriteCommandOperation<BsonDocument> CreateOperation()
        {
            var command = CreateCommand();
            return new WriteCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
        }
    }
}
