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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Internal;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Operations
{
    internal class BulkInsertOperation : BulkUnmixedWriteOperationBase
    {
        // private fields
        private readonly BulkInsertOperationArgs _args;

        // constructors
        public BulkInsertOperation(BulkInsertOperationArgs args)
            : base(args)
        {
            _args = args;
        }

        // protected properties
        protected override string CommandName
        {
            get { return "insert"; }
        }

        protected override string RequestsElementName
        {
            get { return "documents"; }
        }

        // public methods
        public override BulkWriteResult Execute(MongoConnection connection)
        {
            var serverInstance = connection.ServerInstance;
            if (!serverInstance.Supports(FeatureId.WriteCommands))
            {
                var emulator = new BulkInsertOperationEmulator(_args);
                return emulator.Execute(connection);
            }

            return base.Execute(connection);
        }

        // protected methods
        protected override BatchSerializer CreateBatchSerializer()
        {
            return new InsertBatchSerializer(_args);
        }

        protected override IEnumerable<WriteRequest> DecorateRequests(IEnumerable<WriteRequest> requests)
        {
            if (_args.AssignId != null)
            {
                return requests.Select(r => { _args.AssignId((InsertRequest)r); return r; });
            }
            else
            {
                return requests;
            }
        }

        // nested classes
        private class InsertBatchSerializer : BatchSerializer
        {
            // private fields
            private readonly BulkInsertOperationArgs _args;
            private IBsonSerializer _cachedSerializer;
            private Type _cachedSerializerType;

            // constructors
            public InsertBatchSerializer(BulkInsertOperationArgs args)
                : base(args)
            {
                _args = args;
            }

            // protected methods
            protected override void SerializeRequest(BsonBinaryWriter bsonWriter, WriteRequest request)
            {
                var insertRequest = (InsertRequest)request;
                var document = insertRequest.Document;
                if (document == null)
                {
                    throw new ArgumentException("Batch contains one or more null documents.");
                }

                var actualType = document.GetType();

                IBsonSerializer serializer;
                if (actualType == insertRequest.NominalType && insertRequest.Serializer != null)
                {
                    serializer = insertRequest.Serializer;
                }
                else
                {
                    if (_cachedSerializerType != actualType)
                    {
                        _cachedSerializer = BsonSerializer.LookupSerializer(actualType);
                        _cachedSerializerType = actualType;
                    }
                    serializer = _cachedSerializer;
                }
                var serializationOptions = insertRequest.SerializationOptions ?? DocumentSerializationOptions.SerializeIdFirstInstance;

                var savedCheckElementNames = bsonWriter.CheckElementNames;
                try
                {
                    bsonWriter.PushMaxDocumentSize(_args.MaxDocumentSize);
                    bsonWriter.CheckElementNames = _args.CheckElementNames;
                    serializer.Serialize(bsonWriter, insertRequest.NominalType, document, serializationOptions);
                }
                finally
                {
                    bsonWriter.PopMaxDocumentSize();
                    bsonWriter.CheckElementNames = savedCheckElementNames;
                }
            }
        }
    }
}
