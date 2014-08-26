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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    internal class MongoDatabaseImpl : IMongoDatabase
    {
        private readonly ICluster _cluster;
        private readonly string _databaseName;
        private readonly IOperationExecutor _operationExecutor;
        private readonly MongoDatabaseSettings _settings;

        public MongoDatabaseImpl(ICluster cluster, string databaseName, MongoDatabaseSettings settings, IOperationExecutor operationExecutor)
        {
            _cluster = cluster;
            _databaseName = databaseName;
            _settings = settings;
            _operationExecutor = operationExecutor;
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public MongoDatabaseSettings Settings
        {
            get { return _settings; }
        }

        public Task DropAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new DropDatabaseOperation(_databaseName, messageEncoderSettings);
            return ExecuteWriteOperation(operation, timeout, cancellationToken);
        }

        public Task<IReadOnlyList<string>> GetCollectionNamesAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new ListCollectionNamesOperation(_databaseName, messageEncoderSettings);
            return ExecuteReadOperation(operation, timeout, cancellationToken);
        }

        public Task<T> RunCommandAsync<T>(object command, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(command, "command");

            var commandDocument = command as BsonDocument;
            if (commandDocument == null)
            {
                if (command is string)
                {
                    commandDocument = BsonDocument.Parse((string)command);
                }
                else
                {
                    var commandSerializer = _settings.SerializerRegistry.GetSerializer(command.GetType());
                    commandDocument = new BsonDocumentWrapper(command, commandSerializer);
                }
            }

            var isReadCommand = CanCommandBeSentToSecondary.Delegate(commandDocument);
            var serializer = _settings.SerializerRegistry.GetSerializer<T>();
            var messageEncoderSettings = GetMessageEncoderSettings();

            if (isReadCommand)
            {
                var operation = new ReadCommandOperation<T>(_databaseName, commandDocument, serializer, messageEncoderSettings);
                return ExecuteReadOperation<T>(operation, timeout, cancellationToken);
            }
            else
            {
                var operation = new WriteCommandOperation<T>(_databaseName, commandDocument, serializer, messageEncoderSettings);
                return ExecuteWriteOperation<T>(operation, timeout, cancellationToken);
            }
        }

        private async Task<T> ExecuteReadOperation<T>(IReadOperation<T> operation, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // TODO: use settings ReadPreference
            using (var binding = new ReadPreferenceBinding(_cluster, Core.Clusters.ReadPreference.Primary))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, timeout, cancellationToken);
            }
        }

        private async Task<T> ExecuteWriteOperation<T>(IWriteOperation<T> operation, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, timeout, cancellationToken);
            }
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Helper.StrictUtf8Encoding },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Helper.StrictUtf8Encoding }
            };
        }
    }
}
