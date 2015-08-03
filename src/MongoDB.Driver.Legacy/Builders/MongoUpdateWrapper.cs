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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Builders
{
    [BsonSerializer(typeof(MongoUpdateWrapper.Serializer))]
    internal class MongoUpdateWrapper : IMongoUpdate
    {
        // fields
        private Type _nominalType;
        private IBsonSerializer _serializer;
        private readonly object _wrapped;

        // constructors
        public MongoUpdateWrapper(object wrapped, IBsonSerializer serializer, Type nominalType)
        {
            _wrapped = wrapped;
            _serializer = serializer;
            _nominalType = nominalType;
        }

        // nested classes
        internal class Serializer : SerializerBase<MongoUpdateWrapper>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MongoUpdateWrapper value)
            {
                value._serializer.Serialize(context, new BsonSerializationArgs { NominalType = value._nominalType }, value._wrapped);
            }
        }
    }
}
