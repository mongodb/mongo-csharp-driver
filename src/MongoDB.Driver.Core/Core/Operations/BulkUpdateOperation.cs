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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkUpdateOperation : BulkUnmixedWriteOperationBase
    {
        // fields
        private IElementNameValidator _elementNameValidator = NoOpElementNameValidator.Instance;

        // constructors
        public BulkUpdateOperation(
            CollectionNamespace collectionNamespace,
            IEnumerable<UpdateRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, requests, messageEncoderSettings)
        {
        }

        // properties
        protected override string CommandName
        {
            get { return "update"; }
        }

        public IElementNameValidator ElementNameValidator
        {
            get { return _elementNameValidator; }
            set { _elementNameValidator = Ensure.IsNotNull(value, "value"); }
        }

        public new IEnumerable<UpdateRequest> Requests
        {
            get { return base.Requests.Cast<UpdateRequest>(); }
            set { base.Requests = value; }
        }

        protected override string RequestsElementName
        {
            get { return "updates"; }
        }

        // methods
        protected override BatchSerializer CreateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize)
        {
            return new UpdateBatchSerializer(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize, _elementNameValidator);
        }

        protected override BulkUnmixedWriteOperationEmulatorBase CreateEmulator()
        {
            return new BulkUpdateOperationEmulator(CollectionNamespace, Requests, MessageEncoderSettings)
            {
                ElementNameValidator = ElementNameValidator,
                MaxBatchCount = MaxBatchCount,
                MaxBatchLength = MaxBatchLength,
                IsOrdered = IsOrdered,
                WriteConcern = WriteConcern
            };
        }

        // nested types
        private class UpdateBatchSerializer : BatchSerializer
        {
            // fields
            private readonly IElementNameValidator _elementNameValidator;

            // constructors
            public UpdateBatchSerializer(int maxBatchCount, int maxBatchLength, int maxDocumentSize, int maxWireDocumentSize, IElementNameValidator elementNameValidator)
                : base(maxBatchCount, maxBatchLength, maxDocumentSize, maxWireDocumentSize)
            {
                _elementNameValidator = elementNameValidator;
            }

            // methods
            private void SerializeQuery(BsonBinaryWriter bsonWriter, BsonDocument query)
            {
                var context = BsonSerializationContext.CreateRoot<BsonDocument>(bsonWriter);
                BsonDocumentSerializer.Instance.Serialize(context, query);
            }

            protected override void SerializeRequest(BsonSerializationContext context, WriteRequest request)
            {
                var updateRequest = (UpdateRequest)request;
                var bsonWriter = (BsonBinaryWriter)context.Writer;

                bsonWriter.PushMaxDocumentSize(MaxWireDocumentSize);
                try
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteName("q");
                    SerializeQuery(bsonWriter, updateRequest.Query);
                    bsonWriter.WriteName("u");
                    SerializeUpdate(bsonWriter, updateRequest.Update);
                    if (updateRequest.IsMultiUpdate.HasValue)
                    {
                        bsonWriter.WriteBoolean("multi", updateRequest.IsMultiUpdate.Value);
                    }
                    if (updateRequest.IsUpsert.HasValue)
                    {
                        bsonWriter.WriteBoolean("upsert", updateRequest.IsUpsert.Value);
                    }
                    bsonWriter.WriteEndDocument();
                }
                finally
                {
                    bsonWriter.PopMaxDocumentSize();
                }
            }

            private void SerializeUpdate(BsonBinaryWriter bsonWriter, BsonDocument update)
            {
                var updateElementNameValidator = new UpdateOrReplacementElementNameValidator(
                   UpdateElementNameValidator.Instance,
                   _elementNameValidator);

                bsonWriter.PushElementNameValidator(updateElementNameValidator);
                try
                {
                    var context = BsonSerializationContext.CreateRoot<BsonDocument>(bsonWriter);
                    BsonDocumentSerializer.Instance.Serialize(context, update);
                }
                finally
                {
                    bsonWriter.PopElementNameValidator();
                }
            }
        }
    }
}
