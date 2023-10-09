/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Vector search query vector.
    /// </summary>
    public sealed class QueryVector
    {
        /// <summary>
        /// Gets the underlying BSON array.
        /// </summary>
        internal BsonArray Array { get; } // do not make public because in the case of ReadOnlyMemory the BsonArray subclass is not fully functional

        private QueryVector(BsonArray array)
        {
            Ensure.IsNotNullOrEmpty(array, nameof(array));
            Array = array;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryVector" /> class.
        /// </summary>
        /// <param name="readOnlyMemory">The memory.</param>
        public QueryVector(ReadOnlyMemory<double> readOnlyMemory) :
            this(new QueryVectorBsonArray<double>(readOnlyMemory))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryVector" /> class.
        /// </summary>
        /// <param name="readOnlyMemory">The memory.</param>
        public QueryVector(ReadOnlyMemory<float> readOnlyMemory) :
            this(new QueryVectorBsonArray<float>(readOnlyMemory))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryVector" /> class.
        /// </summary>
        /// <param name="readOnlyMemory">The memory.</param>
        public QueryVector(ReadOnlyMemory<int> readOnlyMemory) :
            this(new QueryVectorBsonArray<int>(readOnlyMemory))
        {
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="double"/>[] to <see cref="QueryVector"/>.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryVector(double[] array) => new(array);

        /// <summary>
        /// Performs an implicit conversion from a of <see cref="ReadOnlyMemory{T}"/> to <see cref="QueryVector"/>.
        /// </summary>
        /// <param name="readOnlyMemory">The readOnlyMemory.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryVector(ReadOnlyMemory<double> readOnlyMemory) => new(readOnlyMemory);

        /// <summary>
        /// Performs an implicit conversion from <see cref="float"/>[] to <see cref="QueryVector"/>.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryVector(float[] array) => new(array);

        /// <summary>
        /// Performs an implicit conversion from a of <see cref="ReadOnlyMemory{T}"/> to <see cref="QueryVector"/>.
        /// </summary>
        /// <param name="readOnlyMemory">The readOnlyMemory.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryVector(ReadOnlyMemory<float> readOnlyMemory) => new(readOnlyMemory);

        /// <summary>
        /// Performs an implicit conversion from <see cref="int"/>[] to <see cref="QueryVector"/>.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryVector(int[] array) => new(array);

        /// <summary>
        /// Performs an implicit conversion from a of <see cref="ReadOnlyMemory{T}"/> to <see cref="QueryVector"/>.
        /// </summary>
        /// <param name="readOnlyMemory">The readOnlyMemory.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator QueryVector(ReadOnlyMemory<int> readOnlyMemory) => new(readOnlyMemory);
    }

    // WARNING: this class is a very partial implementation of a BsonArray subclass
    // it is not fully functional and is only intended to be serialized
    [BsonSerializer(typeof(QueryVectorArraySerializer<>))]
    internal sealed class QueryVectorBsonArray<T> : BsonArray
        where T : struct, IConvertible
    {
        private readonly ReadOnlyMemory<T> _memory;

        public QueryVectorBsonArray(ReadOnlyMemory<T> memory)
        {
            _memory = memory;
        }

        // note: Count is only used in tests
        public override int Count => _memory.Length;

        public ReadOnlySpan<T> Span => _memory.Span;

        // note: Values is only used in tests
        public override IEnumerable<BsonValue> Values
        {
            get
            {
                for (int i = 0; i < _memory.Span.Length; i++)
                {
                    yield return _memory.Span[i].ToDouble(null);
                }
            }
        }
    }

    internal sealed class QueryVectorArraySerializer<T> : BsonValueSerializerBase<QueryVectorBsonArray<T>>
        where T : struct, IConvertible
    {
        // constructors
        public QueryVectorArraySerializer()
            : base(BsonType.Array)
        {
        }

        // protected methods
        protected override QueryVectorBsonArray<T> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            throw new NotImplementedException();

        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, QueryVectorBsonArray<T> value)
        {
            var bsonWriter = context.Writer;
            var span = value.Span;

            bsonWriter.WriteStartArray();

            for (int i = 0; i < value.Count; i++)
            {
                bsonWriter.WriteDouble(span[i].ToDouble(null));
            }

            bsonWriter.WriteEndArray();
        }
    }
}
