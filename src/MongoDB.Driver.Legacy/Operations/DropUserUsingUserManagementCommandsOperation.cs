/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Operations
{
    internal class DropUserUsingUserManagementCommandsOperation : IWriteOperation<bool>
    {
        // fields
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly string _username;

        // constructors
        public DropUserUsingUserManagementCommandsOperation(
            DatabaseNamespace databaseNamespace,
            string username,
            MessageEncoderSettings messageEncoderSettings)
        {
            _databaseNamespace = databaseNamespace;
            _username = username;
            _messageEncoderSettings = messageEncoderSettings;
        }

        // methods
        public bool Execute(IWriteBinding binding, CancellationToken cancellationToken)
        {
            var command = new BsonDocument("dropUser", _username);
            var operation = new WriteCommandOperation<BsonDocument>(_databaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings);
            operation.Execute(binding, cancellationToken);
            return true;
        }

        public Task<bool> ExecuteAsync(IWriteBinding binding, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
