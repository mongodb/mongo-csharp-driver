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


namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for BsonValues.
    /// </summary>
    public class BsonValueSerializer : BsonValueSerializerBase<BsonValue>, IBsonArraySerializer, IBsonDocumentSerializer
    {
        // private static fields
        private static BsonValueSerializer __instance = new BsonValueSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonValueSerializer class.
        /// </summary>
        public BsonValueSerializer()
            : base(null)
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonValueSerializer class.
        /// </summary>
        public static BsonValueSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        protected override BsonValue DeserializeValue(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Array: return BsonArraySerializer.Instance.Deserialize(context);
                case BsonType.Binary: return BsonBinaryDataSerializer.Instance.Deserialize(context);
                case BsonType.Boolean: return BsonBooleanSerializer.Instance.Deserialize(context);
                case BsonType.DateTime: return BsonDateTimeSerializer.Instance.Deserialize(context);
                case BsonType.Document: return BsonDocumentSerializer.Instance.Deserialize(context);
                case BsonType.Double: return BsonDoubleSerializer.Instance.Deserialize(context);
                case BsonType.Int32: return BsonInt32Serializer.Instance.Deserialize(context);
                case BsonType.Int64: return BsonInt64Serializer.Instance.Deserialize(context);
                case BsonType.JavaScript: return BsonJavaScriptSerializer.Instance.Deserialize(context);
                case BsonType.JavaScriptWithScope: return BsonJavaScriptWithScopeSerializer.Instance.Deserialize(context);
                case BsonType.MaxKey: return BsonMaxKeySerializer.Instance.Deserialize(context);
                case BsonType.MinKey: return BsonMinKeySerializer.Instance.Deserialize(context);
                case BsonType.Null: return BsonNullSerializer.Instance.Deserialize(context);
                case BsonType.ObjectId: return BsonObjectIdSerializer.Instance.Deserialize(context);
                case BsonType.RegularExpression: return BsonRegularExpressionSerializer.Instance.Deserialize(context);
                case BsonType.String: return BsonStringSerializer.Instance.Deserialize(context);
                case BsonType.Symbol: return BsonSymbolSerializer.Instance.Deserialize(context);
                case BsonType.Timestamp: return BsonTimestampSerializer.Instance.Deserialize(context);
                case BsonType.Undefined: return BsonUndefinedSerializer.Instance.Deserialize(context);

                default:
                    var message = string.Format("Invalid BsonType {0}.", bsonType);
                    throw new BsonInternalException(message);
            }
        }

        /// <summary>
        /// Gets the serialization info for a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>
        /// The serialization info for the member.
        /// </returns>
        public BsonSerializationInfo GetMemberSerializationInfo(string memberName)
        {
            return new BsonSerializationInfo(
                memberName,
                BsonValueSerializer.Instance,
                typeof(BsonValue));
        }

        /// <summary>
        /// Gets the serialization info for individual items of the array.
        /// </summary>
        /// <returns>
        /// The serialization info for the items.
        /// </returns>
        public BsonSerializationInfo GetItemSerializationInfo()
        {
            return new BsonSerializationInfo(
                null,
                BsonValueSerializer.Instance,
                typeof(BsonValue));
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        protected override void SerializeValue(BsonSerializationContext context, BsonValue value)
        {
            switch (value.BsonType)
            {
                case BsonType.Array: BsonArraySerializer.Instance.Serialize(context, (BsonArray)value); break;
                case BsonType.Binary: BsonBinaryDataSerializer.Instance.Serialize(context, (BsonBinaryData)value); break;
                case BsonType.Boolean: BsonBooleanSerializer.Instance.Serialize(context, (BsonBoolean)value); break;
                case BsonType.DateTime: BsonDateTimeSerializer.Instance.Serialize(context, (BsonDateTime)value); break;
                case BsonType.Document: BsonDocumentSerializer.Instance.Serialize(context, (BsonDocument)value); break;
                case BsonType.Double: BsonDoubleSerializer.Instance.Serialize(context, (BsonDouble)value); break;
                case BsonType.Int32: BsonInt32Serializer.Instance.Serialize(context, (BsonInt32)value); break;
                case BsonType.Int64: BsonInt64Serializer.Instance.Serialize(context, (BsonInt64)value); break;
                case BsonType.JavaScript: BsonJavaScriptSerializer.Instance.Serialize(context, (BsonJavaScript)value); break;
                case BsonType.JavaScriptWithScope: BsonJavaScriptWithScopeSerializer.Instance.Serialize(context, (BsonJavaScriptWithScope)value); break;
                case BsonType.MaxKey: BsonMaxKeySerializer.Instance.Serialize(context, (BsonMaxKey)value); break;
                case BsonType.MinKey: BsonMinKeySerializer.Instance.Serialize(context, (BsonMinKey)value); break;
                case BsonType.Null: BsonNullSerializer.Instance.Serialize(context, (BsonNull)value); break;
                case BsonType.ObjectId: BsonObjectIdSerializer.Instance.Serialize(context, (BsonObjectId)value); break;
                case BsonType.RegularExpression: BsonRegularExpressionSerializer.Instance.Serialize(context, (BsonRegularExpression)value); break;
                case BsonType.String: BsonStringSerializer.Instance.Serialize(context, (BsonString)value); break;
                case BsonType.Symbol: BsonSymbolSerializer.Instance.Serialize(context, (BsonSymbol)value); break;
                case BsonType.Timestamp: BsonTimestampSerializer.Instance.Serialize(context, (BsonTimestamp)value); break;
                case BsonType.Undefined: BsonUndefinedSerializer.Instance.Serialize(context, (BsonUndefined)value); break;
                default: throw new BsonInternalException("Invalid BsonType.");
            }
        }
    }
}
