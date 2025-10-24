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

using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class DatabaseExistsOperation : IReadOperation<bool>
    {
        private DatabaseNamespace _databaseNamespace;
        private MessageEncoderSettings _messageEncoderSettings;
        private bool _retryRequested;
        private IBsonSerializationDomain _serializationDomain;

        public DatabaseExistsOperation(DatabaseNamespace databaseNamespace, MessageEncoderSettings messageEncoderSettings, IBsonSerializationDomain serializationDomain)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, nameof(databaseNamespace));
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, nameof(messageEncoderSettings));
            _serializationDomain = Ensure.IsNotNull(serializationDomain, nameof(serializationDomain));
        }

        // EXIT
        public DatabaseExistsOperation(DatabaseNamespace databaseNamespace, MessageEncoderSettings messageEncoderSettings)
            : this(databaseNamespace, messageEncoderSettings, BsonSerializer.DefaultSerializationDomain)
        {
        }


        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public bool RetryRequested
        {
            get { return _retryRequested; }
            set { _retryRequested = value; }
        }

        public bool Execute(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));
            var operation = CreateOperation();
            var result = operation.Execute(operationContext, binding);
            // TODO: CSOT find a way to apply CSOT timeout to ToList as well.
            var list = result.ToList(operationContext.CancellationToken);
            return list.Any(x => x["name"] == _databaseNamespace.DatabaseName);
        }

        public async Task<bool> ExecuteAsync(OperationContext operationContext, IReadBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));
            var operation = CreateOperation();
            var result = await operation.ExecuteAsync(operationContext, binding).ConfigureAwait(false);
            // TODO: CSOT find a way to apply CSOT timeout to ToList as well.
            var list = await result.ToListAsync(operationContext.CancellationToken).ConfigureAwait(false);
            return list.Any(x => x["name"] == _databaseNamespace.DatabaseName);
        }

        private ListDatabasesOperation CreateOperation()
        {
            return new ListDatabasesOperation(_messageEncoderSettings, _serializationDomain)
            {
                RetryRequested = _retryRequested
            };
        }
    }
}
