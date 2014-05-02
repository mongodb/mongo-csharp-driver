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
    /// Represents a serializer for BsonDocumentWrappers.
    /// </summary>
    public class BsonDocumentWrapperSerializer : BsonValueSerializerBase<BsonDocumentWrapper>
    {
        // private static fields
        private static BsonDocumentWrapperSerializer __instance = new BsonDocumentWrapperSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDocumentWrapperSerializer class.
        /// </summary>
        public BsonDocumentWrapperSerializer()
            : base(BsonType.Document)
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonDocumentWrapperSerializer class.
        /// </summary>
        public static BsonDocumentWrapperSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes a class.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override BsonDocumentWrapper Deserialize(BsonDeserializationContext context)
        {
            throw CreateCannotBeDeserializedException();
        }

        // protected methods
        /// <summary>
        /// Deserializes a class.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        protected override BsonDocumentWrapper DeserializeValue(BsonDeserializationContext context)
        {
            throw CreateCannotBeDeserializedException();
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        protected override void SerializeValue(BsonSerializationContext context, BsonDocumentWrapper value)
        {
            var bsonWriter = context.Writer;

            if (value.IsUpdateDocument)
            {
                var savedCheckElementNames = bsonWriter.CheckElementNames;
                var savedCheckUpdateDocument = bsonWriter.CheckUpdateDocument;
                try
                {
                    bsonWriter.CheckElementNames = false;
                    bsonWriter.CheckUpdateDocument = true;
                    context.SerializeWithChildContext(value.Serializer, value.Wrapped);
                }
                finally
                {
                    bsonWriter.CheckElementNames = savedCheckElementNames;
                    bsonWriter.CheckUpdateDocument = savedCheckUpdateDocument;
                }
            }
            else
            {
                context.SerializeWithChildContext(value.Serializer, value.Wrapped);
            }
        }
    }
}
