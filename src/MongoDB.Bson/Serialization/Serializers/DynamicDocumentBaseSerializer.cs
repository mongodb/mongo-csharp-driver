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

using System.Dynamic;
using System.IO;
using System.Linq.Expressions;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Base serializer for dynamic types.
    /// </summary>
    public abstract class DynamicDocumentBaseSerializer<T> : SerializerBase<T> where T : IDynamicMetaObjectProvider
    {
        // private static fields
        private static readonly IBsonSerializer<object> _objectSerializer = BsonSerializer.LookupSerializer<object>();

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicDocumentBaseSerializer{T}"/> class.
        /// </summary>
        protected DynamicDocumentBaseSerializer()
        { }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override T Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            string message;
            switch (bsonType)
            {
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var document = CreateDocument();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = bsonReader.ReadName();
                        var value = context.DeserializeWithChildContext(_objectSerializer, ConfigureDeserializationContext);
                        SetValueForMember(document, name, value);
                    }
                    bsonReader.ReadEndDocument();
                    return document;

                default:
                    message = string.Format("Cannot deserialize a '{0}' from BsonType '{1}'.", BsonUtils.GetFriendlyTypeName(typeof(T)), bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, T value)
        {
            var bsonWriter = context.Writer;

            var metaObject = value.GetMetaObject(Expression.Constant(value));
            var memberNames = metaObject.GetDynamicMemberNames();

            bsonWriter.WriteStartDocument();
            foreach (var memberName in memberNames)
            {
                object memberValue;
                if (TryGetValueForMember(value, memberName, out memberValue))
                {
                    bsonWriter.WriteName(memberName);
                    context.SerializeWithChildContext<object>(_objectSerializer, memberValue, ConfigureSerializationContext);
                }
            }
            bsonWriter.WriteEndDocument();
        }

        // protected methods
        /// <summary>
        /// Configures the deserialization context.
        /// </summary>
        /// <param name="builder">The builder.</param>
        protected abstract void ConfigureDeserializationContext(BsonDeserializationContext.Builder builder);

        /// <summary>
        /// Configures the serialization context.
        /// </summary>
        /// <param name="builder">The builder.</param>
        protected abstract void ConfigureSerializationContext(BsonSerializationContext.Builder builder);

        /// <summary>
        /// Creates the document.
        /// </summary>
        /// <returns>A <typeparamref name="T"/></returns>
        protected abstract T CreateDocument();

        /// <summary>
        /// Sets the value for the member.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="value">The value.</param>
        protected abstract void SetValueForMember(T document, string memberName, object value);

        /// <summary>
        /// Tries to get the value for a member.  Returns true if the member should be serialized.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the member should be serialized; otherwise <c>false</c>.</returns>
        protected abstract bool TryGetValueForMember(T document, string memberName, out object value);
    }
}