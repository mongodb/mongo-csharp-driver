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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Encryption;
using static MongoDB.Driver.Encryption.EncryptedCollectionHelper;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class DropCollectionOperation : IWriteOperation<BsonDocument>
    {
        #region static

        public static IWriteOperation<BsonDocument> CreateEncryptedDropCollectionOperationIfConfigured(
            CollectionNamespace collectionNamespace,
            BsonDocument encryptedFields,
            MessageEncoderSettings messageEncoderSettings,
            Action<DropCollectionOperation> configureDropCollectionConfigurator)
        {
            var mainOperation = new DropCollectionOperation(collectionNamespace, messageEncoderSettings)
            {
                EncryptedFields = encryptedFields
            };

            configureDropCollectionConfigurator?.Invoke(mainOperation);

            if (encryptedFields != null)
            {
                return new CompositeWriteOperation<BsonDocument>(
                    (CreateInnerDropOperation(EncryptedCollectionHelper.GetAdditionalCollectionName(encryptedFields, collectionNamespace, HelperCollectionForEncryption.Esc)), IsMainOperation: false),
                    (CreateInnerDropOperation(EncryptedCollectionHelper.GetAdditionalCollectionName(encryptedFields, collectionNamespace, HelperCollectionForEncryption.Ecos)), IsMainOperation: false),
                    (mainOperation, IsMainOperation: true));
            }
            else
            {
                return mainOperation;
            }

            DropCollectionOperation CreateInnerDropOperation(string collectionName)
                => new DropCollectionOperation(new CollectionNamespace(collectionNamespace.DatabaseNamespace.DatabaseName, collectionName), messageEncoderSettings);
        }
        #endregion

        private readonly CollectionNamespace _collectionNamespace;
        private BsonDocument _encryptedFields;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private WriteConcern _writeConcern;

        public DropCollectionOperation(
            CollectionNamespace collectionNamespace,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _messageEncoderSettings = messageEncoderSettings;
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        internal BsonDocument EncryptedFields
        {
            get { return _encryptedFields; }
            private set { _encryptedFields = value; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public string OperationName => "dropCollection";

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = value; }
        }

        public BsonDocument Execute(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var channelSource = binding.GetWriteChannelSource(operationContext))
            using (var channel = channelSource.GetChannel(operationContext))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session);
                BsonDocument result;
                try
                {
                    result = operation.Execute(operationContext, channelBinding);
                }
                catch (MongoCommandException ex)
                {
                    if (!ShouldIgnoreException(ex))
                    {
                        throw;
                    }
                    result = ex.Result;
                }
                return result;
            }
        }

        public async Task<BsonDocument> ExecuteAsync(OperationContext operationContext, IWriteBinding binding)
        {
            Ensure.IsNotNull(binding, nameof(binding));

            using (BeginOperation())
            using (var channelSource = await binding.GetWriteChannelSourceAsync(operationContext).ConfigureAwait(false))
            using (var channel = await channelSource.GetChannelAsync(operationContext).ConfigureAwait(false))
            using (var channelBinding = new ChannelReadWriteBinding(channelSource.Server, channel, binding.Session.Fork()))
            {
                var operation = CreateOperation(operationContext, channelBinding.Session);
                BsonDocument result;
                try
                {
                    result = await operation.ExecuteAsync(operationContext, channelBinding).ConfigureAwait(false);
                }
                catch (MongoCommandException ex)
                {
                    if (!ShouldIgnoreException(ex))
                    {
                        throw;
                    }
                    result = ex.Result;
                }
                return result;
            }
        }

        internal BsonDocument CreateCommand(OperationContext operationContext, ICoreSessionHandle session)
        {
            var writeConcern = WriteConcernHelper.GetEffectiveWriteConcern(operationContext, session, _writeConcern);
            return new BsonDocument
            {
                { "drop", _collectionNamespace.CollectionName },
                { "writeConcern", writeConcern, writeConcern != null }
            };
        }

        private EventContext.OperationNameDisposer BeginOperation() => EventContext.BeginOperation("drop");

        private WriteCommandOperation<BsonDocument> CreateOperation(OperationContext operationContext, ICoreSessionHandle session)
        {
            var command = CreateCommand(operationContext, session);
            return new WriteCommandOperation<BsonDocument>(_collectionNamespace.DatabaseNamespace, command, BsonDocumentSerializer.Instance, _messageEncoderSettings, OperationName);
        }

        private bool ShouldIgnoreException(MongoCommandException ex)
        {
            return
                ex.Code == (int)ServerErrorCode.NamespaceNotFound ||
                ex.ErrorMessage == "ns not found";
        }
    }
}
