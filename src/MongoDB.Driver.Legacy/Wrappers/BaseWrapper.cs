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

namespace MongoDB.Driver.Wrappers
{
    /// <summary>
    /// Abstract base class for wrapper classes.
    /// </summary>
    [BsonSerializer(typeof(BaseWrapper.Serializer))]
    public abstract class BaseWrapper
    {
        // private fields
        private Type _nominalType;
        private object _wrapped;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BaseWrapper class.
        /// </summary>
        /// <param name="wrapped">The wrapped object.</param>
        protected BaseWrapper(object wrapped)
        {
            _nominalType = wrapped.GetType();
            _wrapped = wrapped;
        }

        /// <summary>
        /// Initializes a new instance of the BaseWrapper class.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="wrapped">The wrapped object.</param>
        protected BaseWrapper(Type nominalType, object wrapped)
        {
            _nominalType = nominalType;
            _wrapped = wrapped;
        }

        // protected methods
        /// <summary>
        /// Serializes the wrapped value.
        /// </summary>
        /// <param name="context">The context.</param>
        protected void SerializeWrappedObject(BsonSerializationContext context)
        {
            var serializer = BsonSerializer.LookupSerializer(_nominalType);
            serializer.Serialize(context, _wrapped);
        }

        // nested classes
        internal class Serializer : SerializerBase<BaseWrapper>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, BaseWrapper value)
            {
                value.SerializeWrappedObject(context);
            }
        }
    }
}
