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
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents a serializer for TClass (a subclass of BsonDocumentBackedClass).
    /// </summary>
    /// <typeparam name="TClass">The subclass of BsonDocumentBackedClass.</typeparam>
    public abstract class BsonDocumentBackedClassSerializer<TClass> : ClassSerializerBase<TClass>, IBsonDocumentSerializer
        where TClass : BsonDocumentBackedClass
    {
        // private fields
        private readonly Dictionary<string, BsonSerializationInfo> _memberSerializationInfo;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDocumentBackedClassSerializer{TClass}"/> class.
        /// </summary>
        protected BsonDocumentBackedClassSerializer()
        {
            _memberSerializationInfo = new Dictionary<string, BsonSerializationInfo>();
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override TClass Deserialize(BsonDeserializationContext context)
        {
            var backingDocument = BsonDocumentSerializer.Instance.Deserialize(context);
            return CreateInstance(backingDocument);
        }

        /// <summary>
        /// Gets the serialization info for a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>
        /// The serialization info for the member.
        /// </returns>
        public virtual BsonSerializationInfo GetMemberSerializationInfo(string memberName)
        {
            BsonSerializationInfo info;
            if (!_memberSerializationInfo.TryGetValue(memberName, out info))
            {
                var message = string.Format("{0} is not a member of {1}.", memberName, typeof(TClass));
                throw new ArgumentOutOfRangeException("memberName", message);
            }

            return info;
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        protected override void SerializeValue(BsonSerializationContext context, TClass value)
        {
            var backingDocument = ((BsonDocumentBackedClass)value).BackingDocument;
            context.SerializeWithChildContext(BsonDocumentSerializer.Instance, backingDocument);
        }

        // protected methods
        /// <summary>
        /// Registers a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <param name="elementName">The element name.</param>
        /// <param name="serializer">The serializer.</param>
        protected void RegisterMember(string memberName, string elementName, IBsonSerializer serializer)
        {
            if (memberName == null)
            {
                throw new ArgumentNullException("memberName");
            }
            if (elementName == null)
            {
                throw new ArgumentNullException("elementName");
            }
            if (serializer == null)
            {
                throw new ArgumentNullException("serializer");
            }

            var info = new BsonSerializationInfo(elementName, serializer, serializer.ValueType);
            _memberSerializationInfo.Add(memberName, info);
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="backingDocument">The backing document.</param>
        /// <returns>An instance of TClass.</returns>
        protected abstract TClass CreateInstance(BsonDocument backingDocument);
    }
}
