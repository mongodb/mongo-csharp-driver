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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public abstract class FindAndModifyOperationBase<TResult> : IWriteOperation<TResult>, ICommandOperation
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly IBsonSerializer<TResult> _resultSerializer;

        // constructors
        public FindAndModifyOperationBase(CollectionNamespace collectionNamespace, IBsonSerializer<TResult> resultSerializer, MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _resultSerializer = Ensure.IsNotNull(resultSerializer, "resultSerializer");
            _messageEncoderSettings = Ensure.IsNotNull(messageEncoderSettings, "messageEncoderSettings");
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
        }

        public IBsonSerializer<TResult> ResultSerializer
        {
            get { return _resultSerializer; }
        }

        // methods
        public abstract BsonDocument CreateCommand();

        public Task<TResult> ExecuteAsync(IWriteBinding binding, TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var nullableDeserializer = new PossiblyNullDeserializer(_resultSerializer);
            var serializer = new ElementDeserializer<TResult>("value", nullableDeserializer);
            var operation = new WriteCommandOperation<TResult>(_collectionNamespace.DatabaseNamespace, command, serializer, _messageEncoderSettings)
            {
                CommandValidator = GetCommandValidator()
            };
            return operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        protected abstract IElementNameValidator GetCommandValidator();

        private class PossiblyNullDeserializer : SerializerBase<TResult>
        {
            private readonly IBsonSerializer<TResult> _resultSerializer;

            public PossiblyNullDeserializer(IBsonSerializer<TResult> resultSerializer)
            {
                _resultSerializer = resultSerializer;
            }

            public override TResult Deserialize(BsonDeserializationContext context)
            {
                var reader = context.Reader;
                if (reader.CurrentBsonType == BsonType.Null)
                {
                    reader.SkipValue();
                    return default(TResult);
                }

                return _resultSerializer.Deserialize(context);
            }
        }
    }
}
