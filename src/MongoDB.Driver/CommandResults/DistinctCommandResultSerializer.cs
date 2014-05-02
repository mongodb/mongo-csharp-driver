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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System.Collections.Generic;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a serializer for a DistinctCommandResult with values of type TValue.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class DistinctCommandResultSerializer<TValue> : SerializerBase<DistinctCommandResult<TValue>>
    {
        // private fields
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DistinctCommandResultSerializer{TValue}"/> class.
        /// </summary>
        public DistinctCommandResultSerializer()
            : this(BsonSerializer.LookupSerializer<TValue>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistinctCommandResultSerializer{TValue}"/> class.
        /// </summary>
        /// <param name="valueSerializer">The value serializer.</param>
        public DistinctCommandResultSerializer(IBsonSerializer<TValue> valueSerializer)
        {
            _valueSerializer = valueSerializer;
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>The value.</returns>
        public override DistinctCommandResult<TValue> Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            var response = new BsonDocument();
            IEnumerable<TValue> values = null;

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = bsonReader.ReadName();
                if (name == "values")
                {
                    values = ReadValues(context);
                }
                else
                {
                    var value = BsonValueSerializer.Instance.Deserialize(context.CreateChild(typeof(BsonValue)));
                    response.Add(name, value);
                }
            }
            bsonReader.ReadEndDocument();

            return new DistinctCommandResult<TValue>(response, values);
        }

        // private methods
        private IEnumerable<TValue> ReadValues(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            var values = new List<TValue>();

            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                values.Add(context.DeserializeWithChildContext(_valueSerializer));
            }
            bsonReader.ReadEndArray();

            return values;
        }
    }
}
