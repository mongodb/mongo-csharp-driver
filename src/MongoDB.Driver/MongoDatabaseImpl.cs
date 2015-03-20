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
    internal sealed class MongoDatabaseImpl : MongoDatabaseBase
    {
        // fields
        private readonly IMongoClient _client;
        private readonly ICluster _cluster;
        private readonly DatabaseNamespace _databaseNamespace;
        private readonly IOperationExecutor _operationExecutor;
        private readonly MongoDatabaseSettings _settings;

        // constructors
        public MongoDatabaseImpl(IMongoClient client, DatabaseNamespace databaseNamespace, MongoDatabaseSettings settings, ICluster cluster, IOperationExecutor operationExecutor)
        {
            _client = Ensure.IsNotNull(client, "client");
            _databaseNamespace = Ensure.IsNotNull(databaseNamespace, "databaseNamespace");
            _settings = Ensure.IsNotNull(settings, "settings").Freeze();
            _cluster = Ensure.IsNotNull(cluster, "cluster");
            _operationExecutor = Ensure.IsNotNull(operationExecutor, "operationExecutor");
        }

        // properties
        public override IMongoClient Client
        {
            get { return _client; }
        }

        public override DatabaseNamespace DatabaseNamespace
        {
            get { return _databaseNamespace; }
        }

        public override MongoDatabaseSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public override Task CreateCollectionAsync(string name, CreateCollectionOptions options, CancellationToken cancellationToken)
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
                StorageEngine = options.StorageEngine,
                UsePowerOf2Sizes = options.UsePowerOf2Sizes
            };

            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public override Task DropCollectionAsync(string name, CancellationToken cancellationToken)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new DropCollectionOperation(new CollectionNamespace(_databaseNamespace, name), messageEncoderSettings);
            return ExecuteWriteOperation(operation, cancellationToken);
        }

        public override IMongoCollection<TDocument> GetCollection<TDocument>(string name, MongoCollectionSettings settings)
        {
            Ensure.IsNotNullOrEmpty(name, "name");

            settings = settings == null ?
                new MongoCollectionSettings() :
                settings.Clone();

            settings.ApplyDefaultValues(_settings);

            return new MongoCollectionImpl<TDocument>(this, new CollectionNamespace(_databaseNamespace, name), settings, _cluster, _operationExecutor);
        }

        public override Task<IAsyncCursor<BsonDocument>> ListCollectionsAsync(ListCollectionsOptions options, CancellationToken cancellationToken)
        {
            var messageEncoderSettings = GetMessageEncoderSettings();
            var operation = new ListCollectionsOperation(_databaseNamespace, messageEncoderSettings)
            {
                Filter = options == null ? null : options.Filter.Render(_settings.SerializerRegistry.GetSerializer<BsonDocument>(), _settings.SerializerRegistry)
            };
            return ExecuteReadOperation(operation, ReadPreference.Primary, cancellationToken);
        }

        public override Task RenameCollectionAsync(string oldName, string newName, RenameCollectionOptions options, CancellationToken cancellationToken)
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

        public override Task<TResult> RunCommandAsync<TResult>(Command<TResult> command, ReadPreference readPreference = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(command, "command");
            readPreference = readPreference ?? ReadPreference.Primary;

            var renderedCommand = command.Render(_settings.SerializerRegistry);
            var messageEncoderSettings = GetMessageEncoderSettings();

            if (readPreference == ReadPreference.Primary)
            {
                var operation = new WriteCommandOperation<TResult>(_databaseNamespace, renderedCommand.Document, renderedCommand.ResultSerializer, messageEncoderSettings);
                return ExecuteWriteOperation<TResult>(operation, cancellationToken);
            }
            else
            {
                var operation = new ReadCommandOperation<TResult>(_databaseNamespace, renderedCommand.Document, renderedCommand.ResultSerializer, messageEncoderSettings);
                return ExecuteReadOperation<TResult>(operation, readPreference, cancellationToken);
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
                return await _operationExecutor.ExecuteReadOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<T> ExecuteWriteOperation<T>(IWriteOperation<T> operation, CancellationToken cancellationToken)
        {
            using (var binding = new WritableServerBinding(_cluster))
            {
                return await _operationExecutor.ExecuteWriteOperationAsync(binding, operation, cancellationToken).ConfigureAwait(false);
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
