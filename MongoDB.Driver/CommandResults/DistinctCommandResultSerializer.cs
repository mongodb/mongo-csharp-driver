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
    public class DistinctCommandResultSerializer<TValue> : BsonBaseSerializer
    {
        // private fields
        private readonly IBsonSerializer _valueSerializer;
        private readonly IBsonSerializationOptions _valueSerializationOptions;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DistinctCommandResultSerializer{TValue}"/> class.
        /// </summary>
        public DistinctCommandResultSerializer()
            : this(BsonSerializer.LookupSerializer(typeof(TValue)), null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DistinctCommandResultSerializer{TValue}"/> class.
        /// </summary>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <param name="valueSerializationOptions">The value serialization options.</param>
        public DistinctCommandResultSerializer(IBsonSerializer valueSerializer, IBsonSerializationOptions valueSerializationOptions)
        {
            _valueSerializer = valueSerializer;
            _valueSerializationOptions = valueSerializationOptions;
        }

        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>
        /// An object.
        /// </returns>
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            var response = new BsonDocument();
            IEnumerable<TValue> values = null;

            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var name = bsonReader.ReadName();
                if (name == "values")
                {
                    values = ReadValues(bsonReader);
                }
                else
                {
                    var value = (BsonValue)BsonValueSerializer.Instance.Deserialize(bsonReader, typeof(BsonValue), null);
                    response.Add(name, value);
                }
            }
            bsonReader.ReadEndDocument();

            return new DistinctCommandResult<TValue>(response, values);
        }

        // private methods
        private IEnumerable<TValue> ReadValues(BsonReader bsonReader)
        {
            var values = new List<TValue>();

            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                values.Add((TValue)_valueSerializer.Deserialize(bsonReader, typeof(TValue), _valueSerializationOptions));
            }
            bsonReader.ReadEndArray();

            return values;
        }
    }
}
