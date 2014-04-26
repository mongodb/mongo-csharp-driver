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
using System.IO;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for BsonJavaScripts.
    /// </summary>
    public class BsonJavaScriptSerializer : BsonBaseSerializer<BsonJavaScript>
    {
        // private static fields
        private static BsonJavaScriptSerializer __instance = new BsonJavaScriptSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonJavaScriptSerializer class.
        /// </summary>
        public BsonJavaScriptSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonJavaScriptSerializer class.
        /// </summary>
        public static BsonJavaScriptSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override BsonJavaScript Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.JavaScript:
                    var code = bsonReader.ReadJavaScript();
                    return new BsonJavaScript(code);

                default:
                    var message = string.Format("Cannot deserialize BsonJavaScript from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonJavaScript value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            bsonWriter.WriteJavaScript(value.Code);
        }
    }
}
