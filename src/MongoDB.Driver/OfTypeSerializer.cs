/* Copyright 2015-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    internal sealed class OfTypeSerializer<TRootDocument, TDerivedDocument> : SerializerBase<TDerivedDocument>, IBsonDocumentSerializer, IBsonIdProvider
        where TDerivedDocument : TRootDocument
    {
        private readonly IBsonSerializer<TDerivedDocument> _derivedDocumentSerializer;

        public OfTypeSerializer(IBsonSerializer<TDerivedDocument> derivedDocumentSerializer)
        {
            _derivedDocumentSerializer = derivedDocumentSerializer;
        }

        public override TDerivedDocument Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            args.NominalType = typeof(TRootDocument);
            return _derivedDocumentSerializer.Deserialize(context, args);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is OfTypeSerializer<TRootDocument, TDerivedDocument> other &&
                object.Equals(_derivedDocumentSerializer, other._derivedDocumentSerializer);
        }

        public bool
            GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
                => GetDocumentId(document, BsonSerializer.DefaultSerializationDomain, out id, out idNominalType, out idGenerator);

        public bool GetDocumentId(object document, IBsonSerializationDomain domain, out object id, out Type idNominalType,
            out IIdGenerator idGenerator)
        {
            if (_derivedDocumentSerializer is IBsonIdProvider idProvider)
            {
                return idProvider.GetDocumentId(document, out id, out idNominalType, out idGenerator);
            }
            else
            {
                id = null;
                idNominalType = null;
                idGenerator = null;
                return false;
            }
        }

        public override int GetHashCode() => 0;

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TDerivedDocument value)
        {
            args.NominalType = typeof(TRootDocument);
            _derivedDocumentSerializer.Serialize(context, args, value);
        }

        public void SetDocumentId(object document, object id)
        {
            if (_derivedDocumentSerializer is IBsonIdProvider idProvider)
            {
                idProvider.SetDocumentId(document, id);
            }
        }

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            var documentSerializer = _derivedDocumentSerializer as IBsonDocumentSerializer;
            if (documentSerializer == null)
            {
                serializationInfo = null;
                return false;
            }

            return documentSerializer.TryGetMemberSerializationInfo(memberName, out serializationInfo);
        }
    }
}
