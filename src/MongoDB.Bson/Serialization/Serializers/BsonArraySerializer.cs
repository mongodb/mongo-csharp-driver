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
    /// Represents a serializer for BsonArrays.
    /// </summary>
    public class BsonArraySerializer : BsonValueSerializerBase<BsonArray>, IBsonArraySerializer
    {
        // private static fields
        private static BsonArraySerializer __instance = new BsonArraySerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonArraySerializer class.
        /// </summary>
        public BsonArraySerializer()
            : base(BsonType.Array)
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonArraySerializer class.
        /// </summary>
        public static BsonArraySerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        protected override BsonArray DeserializeValue(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            bsonReader.ReadStartArray();
            var array = new BsonArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var item = context.DeserializeWithChildContext(BsonValueSerializer.Instance);
                array.Add(item);
            }
            bsonReader.ReadEndArray();

            return array;
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

        // protected methods
        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        protected override void SerializeValue(BsonSerializationContext context, BsonArray value)
        {
            var bsonWriter = context.Writer;

            bsonWriter.WriteStartArray();
            for (int i = 0; i < value.Count; i++)
            {
                context.SerializeWithChildContext(BsonValueSerializer.Instance, value[i]);
            }
            bsonWriter.WriteEndArray();
        }
    }
}
