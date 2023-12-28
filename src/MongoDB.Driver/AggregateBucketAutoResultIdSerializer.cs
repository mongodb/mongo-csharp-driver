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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Static factory class for AggregateBucketAutoResultIdSerializer.
    /// </summary>
    public static class AggregateBucketAutoResultIdSerializer
    {
        /// <summary>
        /// Creates an instance of AggregateBucketAutoResultIdSerializer.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <returns>A AggregateBucketAutoResultIdSerializer.</returns>
        public static IBsonSerializer<AggregateBucketAutoResultId<TValue>> Create<TValue>(IBsonSerializer<TValue> valueSerializer)
        {
            return new AggregateBucketAutoResultIdSerializer<TValue>(valueSerializer);
        }
    }

    /// <summary>
    /// A serializer for AggregateBucketAutoResultId.
    /// </summary>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public class AggregateBucketAutoResultIdSerializer<TValue> : ClassSerializerBase<AggregateBucketAutoResultId<TValue>>, IBsonDocumentSerializer
    {
        private readonly IBsonSerializer<TValue> _valueSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateBucketAutoResultIdSerializer{TValue}"/> class.
        /// </summary>
        /// <param name="valueSerializer">The value serializer.</param>
        public AggregateBucketAutoResultIdSerializer(IBsonSerializer<TValue> valueSerializer)
        {
            _valueSerializer = Ensure.IsNotNull(valueSerializer, nameof(valueSerializer));
        }

        // public methods
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is AggregateBucketAutoResultIdSerializer<TValue> other &&
                object.Equals(_valueSerializer, other._valueSerializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        // protected methods
        /// <inheritdoc/>
        protected override AggregateBucketAutoResultId<TValue> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartDocument();
            TValue min = default;
            TValue max = default;
            while (reader.ReadBsonType() != 0)
            {
                var name = reader.ReadName();
                switch (name)
                {
                    case "min": min = _valueSerializer.Deserialize(context); break;
                    case "max": max = _valueSerializer.Deserialize(context); break;
                    default: throw new BsonSerializationException($"Invalid element name for AggregateBucketAutoResultId: {name}.");
                }
            }
            reader.ReadEndDocument();
            return new AggregateBucketAutoResultId<TValue>(min, max);
        }

        /// <inheritdoc/>
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, AggregateBucketAutoResultId<TValue> value)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();
            writer.WriteName("min");
            _valueSerializer.Serialize(context, value.Min);
            writer.WriteName("max");
            _valueSerializer.Serialize(context, value.Max);
            writer.WriteEndDocument();
        }

        /// <inheritdoc/>
        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = memberName switch
            {
                "Min" => new BsonSerializationInfo("min", _valueSerializer, _valueSerializer.ValueType),
                "Max" => new BsonSerializationInfo("max", _valueSerializer, _valueSerializer.ValueType),
                _ => null
            };
            return serializationInfo != null;
        }
    }
}
