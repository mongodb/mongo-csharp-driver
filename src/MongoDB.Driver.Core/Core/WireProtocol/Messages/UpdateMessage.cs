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


using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class UpdateMessage : RequestMessage
    {
        // fields
        private readonly CollectionNamespace _collectionNamespace;
        private readonly bool _isMulti;
        private readonly bool _isUpsert;
        private readonly BsonDocument _query;
        private readonly BsonDocument _update;
        private readonly IElementNameValidator _updateValidator;

        // constructors
        public UpdateMessage(
            int requestId,
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument update,
            IElementNameValidator updateValidator,
            bool isMulti,
            bool isUpsert)
            : base(requestId)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _query = Ensure.IsNotNull(query, "query");
            _update = Ensure.IsNotNull(update, "update");
            _updateValidator = Ensure.IsNotNull(updateValidator, "updateValidator");
            _isMulti = isMulti;
            _isUpsert = isUpsert;
        }

        // properties
        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public bool IsMulti
        {
            get { return _isMulti; }
        }

        public bool IsUpsert
        {
            get { return _isUpsert; }
        }

        public BsonDocument Query
        {
            get { return _query; }
        }

        public BsonDocument Update
        {
            get { return _update; }
        }

        public IElementNameValidator UpdateValidator
        {
            get { return _updateValidator; }
        }

        // methods
        public new IMessageEncoder<UpdateMessage> GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetUpdateMessageEncoder();
        }

        protected override IMessageEncoder GetNonGenericEncoder(IMessageEncoderFactory encoderFactory)
        {
            return GetEncoder(encoderFactory);
        }
    }
}
