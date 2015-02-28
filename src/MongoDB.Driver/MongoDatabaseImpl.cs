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
    internal sealed class MongoDatabaseImpl : IMongoDatabase
    {
        // fields
        private readonly ICluster _cluster;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly IOperationExecutor _operationExecutor;
        private readonly MongoDatabaseSettings _settings;

        // constructors
        public MongoDatabaseImpl(DatabaseNamespace databaseNamespace, MongoDatabaseSettings settings, ICluster cluster, IOperationExecutor operationExecutor)
        {
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _settings = Ensure.IsNotNull(settings, "settings").Freeze();
            _cluster = Ensure.IsNotNull(cluster, "cluster");
            _operationExecutor = Ensure.IsNotNull(operationExecutor, "operationExecutor");
        }

        // properties
        public DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public MongoDatabaseSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public Task CreateCollectionAsync(string name, CreateCollectionOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNullOrEmpty(name, "name");
            options = options ?? new CreateCollectionOptions();
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new CreateCollectionOperation(new CollectionNamespace(_databaseNamespace, name), messageEncoderSettings)
            {
                AutoIndexId = options.AutoIndexId,
                Capped = options.Capped,
                MaxDocuments = options.MaxDocuments,
                MaxSize = options.MaxSize,
                StorageEngine = BsonDocumentHelper.ToBsonDocument(_settings.SerializerRegistry, options.StorageEngine),
                UsePowerOf2Sizes = options.UsePowerOf2Sizes
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public Task DropCollectionAsync(string name, CancellationToken cancellationToken)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new DropCollectionOperation(new CollectionNamespace(_databaseNamespace, name), messageEncoderSettings);
            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings)
        {
            Ensure.IsNotNullOrEmpty(name, "name");

            settings = settings == null ?
                new MongoCollectionSettings() :
                settings.Clone();

            settings.ApplyDefaultValues(_settings);

            return new MongoCollectionImpl<TDocument>(new CollectionNamespace(_databaseNamespace, name), settings, _cluster, _operationExecutor);
        }

        public Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new ListCollectionsOperation(_databaseNamespace, messageEncoderSettings)
            {
                Filter = options == null ? null : BsonDocumentHelper.FilterToBsonDocument<BsonDocument>(_settings.SerializerRegistry, options.Filter)
            };
            return ExecuteReadOperation(operation, ReadPreference.Primary, cancellationToken);
        }

        public Task RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
        {
            Ensure.IsNotNullOrEmpty(oldName, "oldName");
            Ensure.IsNotNullOrEmpty(newName, "newName");

            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new RenameCollectionOperation(
                new CollectionNamespace(_databaseNamespace, oldName),
                new CollectionNamespace(_databaseNamespace, newName),
                messageEncoderSettings)
            {
                DropTarget = options == null ? null : options.DropTarget
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public Task<T> RunCommandAsync<T>(object command, ReadPreference readPreference = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(command, "command");
            readPreference = readPreference ?? ReadPreference.Primary;

            var commandDocument = BsonDocumentHelper.ToBsonDocument(_settings.SerializerRegistry, command);
            var serializer = _settings.SerializerRegistry.GetSerializer<T>();
            var messageEncoderSettings = GetMessageEncoderSettings();

            if (readPreference == ReadPreference.Primary)
            {
                var operation = new WriteCommandOperation<T>(_databaseNamespace, commandDocument, serializer, messageEncoderSettings);
                return ExecuteWriteOperation<T>(operation, cancellationToken);
            }
            else
            {
                var operation = new ReadCommandOperation<T>(_databaseNamespace, commandDocument, serializer, messageEncoderSettings);
                return ExecuteReadOperation<T>(operation, readPreference, cancellationToken);
            }
        }

        private Task<T> ExecuteReadOperation<T>(IReadOperation<T> operation, CancellationToken cancellationToken)
        {
            return ExecuteReadOperation(operation, _settings.ReadPreference, cancellationToken);
        }

        private async Task<T> ExecuteReadOperation<T>(IReadOperation<T> operation, ReadPreference readPreference, CancellationToken cancellationToken)
        {
            using (var binding = new ReadPreferenceBinding(_cluster, readPreference))
            {
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, _settings.OperationTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<T> ExecuteWriteOperation<T>(IWriteOperation<T> operation, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, _settings.OperationTimeout, cancellationToken).ConfigureAwait(false);
            }
        }

        private MessageEncoderSettings GetMessageEncoderSettings()
        {
            return new MessageEncoderSettings
            {
                { MessageEncoderSettingsName.GuidRepresentation, _settings.GuidRepresentation },
                { MessageEncoderSettingsName.ReadEncoding, _settings.ReadEncoding ?? Utf8Encodings.Strict },
                { MessageEncoderSettingsName.WriteEncoding, _settings.WriteEncoding ?? Utf8Encodings.Strict }
            };
        }
    }
}
