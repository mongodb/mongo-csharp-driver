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
using MongoDB.Bson.ObjectModel;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for BSON vector to/from a given collection.
    /// </summary>
    /// <typeparam name="TItemCollection">The collection type.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public abstract class BsonVectorSerializerBase<TItemCollection, TItem> : SerializerBase<TItemCollection>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorSerializerBase{TItemCollection, TItem}"/> class.
        /// </summary>
        /// <param name="bsonVectorDataType">Type of the bson vector data.</param>
        public BsonVectorSerializerBase(BsonVectorDataType bsonVectorDataType)
        {
            BsonVectorReader.ValidateDataType<TItem>(bsonVectorDataType);

            VectorDataType = bsonVectorDataType;
        }

        /// <summary>
        /// Gets the type of the vector data.
        /// </summary>
        public BsonVectorDataType VectorDataType { get; }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;
    }

    /// <summary>
    /// Represents a serializer for <see cref="BsonVector{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItemCollection">The concrete type derived from <see cref="BsonVector{T}"/>.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class BsonVectorSerializer<TItemCollection, TItem> : BsonVectorSerializerBase<TItemCollection, TItem>
        where TItemCollection : BsonVector<TItem>
        where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadonlyMemorySerializer{TItem}" /> class.
        /// </summary>
        public BsonVectorSerializer(BsonVectorDataType bsonVectorDataType) :
            base(bsonVectorDataType)
        {
        }

        /// <inheritdoc/>
        public override sealed TItemCollection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            var bsonType = reader.GetCurrentBsonType();
            if (bsonType != BsonType.Binary)
            {
                throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }

            var binaryData = reader.ReadBinaryData();
            return binaryData.ToBsonVector<TItem>() as TItemCollection;
        }

        /// <inheritdoc/>
        public override sealed void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TItemCollection bsonVector)
        {
            var binaryData = bsonVector.ToBsonBinaryData();

            context.Writer.WriteBinaryData(binaryData);
        }
    }

    /// <summary>
    /// Represents a base class for serializers to/from collection of <typeparamref name="TItem"/>.
    /// </summary>
    /// <typeparam name="TItemCollection">The collection type.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public abstract class BsonVectorToCollectionSerializer<TItemCollection, TItem> : BsonVectorSerializerBase<TItemCollection, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorToCollectionSerializer{TItemCollection, TItem}" /> class.
        /// </summary>
        public BsonVectorToCollectionSerializer(BsonVectorDataType bsonVectorDataType) :
            base(bsonVectorDataType)
        {

        }

        /// <inheritdoc/>
        public override sealed TItemCollection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            var bsonType = reader.GetCurrentBsonType();
            if (bsonType != BsonType.Binary)
            {
                throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }

            var binaryData = reader.ReadBinaryData();
            var (elements, _, _) = binaryData.ToBsonVectorAsArray<TItem>();

            return CreateResult(elements);
        }

        /// <inheritdoc/>
        public override sealed void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TItemCollection value)
        {
            byte padding = 0;
            if (value is BsonVectorPackedBit bsonVectorPackedBit)
            {
                padding = bsonVectorPackedBit.Padding;
            }

            var vectorData = GetSpan(value);
            var bytes = BsonVectorWriter.WriteVectorData(vectorData, VectorDataType, padding);
            var binaryData = new BsonBinaryData(bytes, BsonBinarySubType.Vector);

            context.Writer.WriteBinaryData(binaryData);
        }

        private protected abstract TItemCollection CreateResult(TItem[] elements);
        private protected abstract ReadOnlySpan<TItem> GetSpan(TItemCollection data);
    }

    /// <summary>
    /// Represents a serializer for BSON vector to/from array of <typeparamref name="TItem"/>.
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class BsonVectorArraySerializer<TItem> : BsonVectorToCollectionSerializer<TItem[], TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorArraySerializer{TItem}" /> class.
        /// </summary>
        public BsonVectorArraySerializer(BsonVectorDataType bsonVectorDataType) : base(bsonVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetSpan(TItem[] data) => data;
        private protected override TItem[] CreateResult(TItem[] elements) => elements;
    }

    /// <summary>
    /// Represents a serializer for BSON vector to/from <see cref="Memory{TItem}"/>
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class BsonVectorMemorySerializer<TItem> : BsonVectorToCollectionSerializer<Memory<TItem>, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorMemorySerializer{TItem}" /> class.
        /// </summary>
        public BsonVectorMemorySerializer(BsonVectorDataType bsonVectorDataType) : base(bsonVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetSpan(Memory<TItem> data) =>
            data.Span;

        private protected override Memory<TItem> CreateResult(TItem[] elements) =>
            new(elements);
    }

    /// <summary>
    /// Represents a serializer for <see cref="ReadOnlyMemory{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class BsonVectorReadOnlyMemorySerializer<TItem> : BsonVectorToCollectionSerializer<ReadOnlyMemory<TItem>, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorReadOnlyMemorySerializer{TItem}" /> class.
        /// </summary>
        public BsonVectorReadOnlyMemorySerializer(BsonVectorDataType bsonVectorDataType) : base(bsonVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetSpan(ReadOnlyMemory<TItem> data) =>
            data.Span;

        private protected override ReadOnlyMemory<TItem> CreateResult(TItem[] elements) =>
            new(elements);
    }
}
