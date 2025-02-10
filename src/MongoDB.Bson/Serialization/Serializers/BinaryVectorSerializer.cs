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
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    internal static class BinaryVectorSerializer
    {
        public static BinaryVectorSerializer<BinaryVectorFloat32, float> BinaryVectorFloat32Serializer { get; } = new BinaryVectorSerializer<BinaryVectorFloat32, float>(BinaryVectorDataType.Float32);
        public static BinaryVectorSerializer<BinaryVectorInt8, sbyte> BinaryVectorInt8Serializer { get; } = new BinaryVectorSerializer<BinaryVectorInt8, sbyte>(BinaryVectorDataType.Int8);
        public static BinaryVectorSerializer<BinaryVectorPackedBit, byte> BinaryVectorPackedBitSerializer { get; } = new BinaryVectorSerializer<BinaryVectorPackedBit, byte>(BinaryVectorDataType.PackedBit);

        public static IBsonSerializer CreateArraySerializer(Type itemType, BinaryVectorDataType binaryVectorDataType) =>
            CreateSerializerInstance(typeof(ArrayAsBinaryVectorSerializer<>).MakeGenericType(itemType), binaryVectorDataType);

        public static IBsonSerializer CreateBinaryVectorSerializer(Type binaryVectorType, Type itemType, BinaryVectorDataType binaryVectorDataType) =>
            CreateSerializerInstance(typeof(BinaryVectorSerializer<,>).MakeGenericType(binaryVectorType, itemType), binaryVectorDataType);

        public static IBsonSerializer CreateMemorySerializer(Type itemType, BinaryVectorDataType binaryVectorDataType) =>
            CreateSerializerInstance(typeof(MemoryAsBinaryVectorSerializer<>).MakeGenericType(itemType), binaryVectorDataType);

        public static IBsonSerializer CreateReadonlyMemorySerializer(Type itemType, BinaryVectorDataType binaryVectorDataType) =>
            CreateSerializerInstance(typeof(ReadOnlyMemoryAsBinaryVectorSerializer<>).MakeGenericType(itemType), binaryVectorDataType);

        public static IBsonSerializer CreateSerializer(Type type, BinaryVectorDataType binaryVectorDataType)
        {
            // Arrays
            if (type.IsArray)
            {
                var itemType = type.GetElementType();
                return CreateArraySerializer(itemType, binaryVectorDataType);
            }

            // BinaryVector
            if (type == typeof(BinaryVectorFloat32) ||
                type == typeof(BinaryVectorInt8) ||
                type == typeof(BinaryVectorPackedBit))
            {
                return CreateBinaryVectorSerializer(type, GetItemType(type.BaseType), binaryVectorDataType);
            }

            // Memory/ReadonlyMemory
            var genericTypeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (genericTypeDefinition == typeof(Memory<>))
            {
                return CreateMemorySerializer(GetItemType(type), binaryVectorDataType);
            }
            else if (genericTypeDefinition == typeof(ReadOnlyMemory<>))
            {
                return CreateReadonlyMemorySerializer(GetItemType(type), binaryVectorDataType);
            }

            throw new NotSupportedException($"Type {type} cannot be serialized as a binary vector.");

            Type GetItemType(Type containerType)
            {
                var genericArguments = containerType.GetGenericArguments();
                if (genericArguments.Length != 1)
                {
                    throw new NotSupportedException($"Type {type} cannot be serialized as a binary vector.");
                }

                return genericArguments[0];
            }
        }

        private static IBsonSerializer CreateSerializerInstance(Type serializerType, BinaryVectorDataType binaryVectorDataType) =>
             (IBsonSerializer)Activator.CreateInstance(serializerType, binaryVectorDataType);
    }

    /// <summary>
    /// Represents a serializer for TItemContainer values represented as a BinaryVector.
    /// </summary>
    /// <typeparam name="TItemContainer">The items container, for example <see cref="BinaryVector{TItem}"/> or <see cref="Memory{TItem}"/>.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public abstract class BinaryVectorSerializerBase<TItemContainer, TItem> : SerializerBase<TItemContainer>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryVectorSerializerBase{TItemContainer, TItem}"/> class.
        /// </summary>
        /// <param name="binaryVectorDataType">Type of the binary vector data.</param>
        private protected BinaryVectorSerializerBase(BinaryVectorDataType binaryVectorDataType)
        {
            BinaryVectorReader.ValidateItemType<TItem>(binaryVectorDataType);

            VectorDataType = binaryVectorDataType;
        }

        /// <summary>
        /// Gets the type of the vector data.
        /// </summary>
        public BinaryVectorDataType VectorDataType { get; }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is BinaryVectorSerializerBase<TItemContainer, TItem> other &&
                object.Equals(VectorDataType, other.VectorDataType);
        }

        /// <summary>
        /// Reads bson binary data.
        /// </summary>
        /// <param name="bsonReader">The bson reader.</param>
        protected BsonBinaryData ReadAndValidateBsonBinaryData(IBsonReader bsonReader)
        {
            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType != BsonType.Binary)
            {
                throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }

            var binaryData = bsonReader.ReadBinaryData();

            return binaryData;
        }
    }

    /// <summary>
    /// Represents a serializer for <see cref="BinaryVector{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItemContainer">The concrete type derived from <see cref="BinaryVector{TItem}"/>.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class BinaryVectorSerializer<TItemContainer, TItem> : BinaryVectorSerializerBase<TItemContainer, TItem>
        where TItemContainer : BinaryVector<TItem>
        where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryVectorSerializer{TItemContainer, TItem}" /> class.
        /// </summary>
        public BinaryVectorSerializer(BinaryVectorDataType binaryVectorDataType) :
            base(binaryVectorDataType)
        {
        }

        /// <inheritdoc/>
        public override TItemContainer Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var binaryData = ReadAndValidateBsonBinaryData(context.Reader);
            return (TItemContainer)binaryData.ToBinaryVector<TItem>();
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TItemContainer value)
        {
            var binaryData = value.ToBsonBinaryData();

            context.Writer.WriteBinaryData(binaryData);
        }
    }

    /// <summary>
    /// A base class for serializers for <typeparamref name="TItem"/> containers represented as a BinaryVector.
    /// </summary>
    /// <typeparam name="TItemContainer">The collection type.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public abstract class ItemContainerAsBinaryVectorSerializer<TItemContainer, TItem> : BinaryVectorSerializerBase<TItemContainer, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerAsBinaryVectorSerializer{TItemContainer, TItem}" /> class.
        /// </summary>
        protected ItemContainerAsBinaryVectorSerializer(BinaryVectorDataType binaryVectorDataType) :
            base(binaryVectorDataType)
        {
        }

        /// <inheritdoc/>
        public sealed override TItemContainer Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var binaryData = ReadAndValidateBsonBinaryData(context.Reader);
            var (items, padding, _) = binaryData.ToBinaryVectorAsArray<TItem>();

            if (padding != 0)
            {
                throw new FormatException($"Non-zero padding is supported only in {nameof(BinaryVectorPackedBit)} data type.");
            }

            return CreateResult(items);
        }

        /// <inheritdoc/>
        public sealed override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TItemContainer value)
        {
            var vectorData = GetItemsSpan(value);
            var bytes = BinaryVectorWriter.WriteToBytes(vectorData, VectorDataType, 0);
            var binaryData = new BsonBinaryData(bytes, BsonBinarySubType.Vector);

            context.Writer.WriteBinaryData(binaryData);
        }

        private protected abstract TItemContainer CreateResult(TItem[] items);
        private protected abstract ReadOnlySpan<TItem> GetItemsSpan(TItemContainer data);
    }

    /// <summary>
    /// Represents a serializer for <typeparamref name="TItem"/> arrays represented as a BinaryVector.
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class ArrayAsBinaryVectorSerializer<TItem> : ItemContainerAsBinaryVectorSerializer<TItem[], TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayAsBinaryVectorSerializer{TItem}" /> class.
        /// </summary>
        public ArrayAsBinaryVectorSerializer(BinaryVectorDataType binaryVectorDataType) : base(binaryVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetItemsSpan(TItem[] data) => data;

        private protected override TItem[] CreateResult(TItem[] items) => items;
    }

    /// <summary>
    /// Represents a serializer for <see cref="Memory{TItem}"/> represented as a binary vector.
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class MemoryAsBinaryVectorSerializer<TItem> : ItemContainerAsBinaryVectorSerializer<Memory<TItem>, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryAsBinaryVectorSerializer{TItem}" /> class.
        /// </summary>
        public MemoryAsBinaryVectorSerializer(BinaryVectorDataType binaryVectorDataType) : base(binaryVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetItemsSpan(Memory<TItem> data) => data.Span;

        private protected override Memory<TItem> CreateResult(TItem[] items) => new(items);
    }

    /// <summary>
    /// Represents a serializer for <see cref="ReadOnlyMemory{TItem}"/> represented as a BinaryVector.
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class ReadOnlyMemoryAsBinaryVectorSerializer<TItem> : ItemContainerAsBinaryVectorSerializer<ReadOnlyMemory<TItem>, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemoryAsBinaryVectorSerializer{TItem}" /> class.
        /// </summary>
        public ReadOnlyMemoryAsBinaryVectorSerializer(BinaryVectorDataType binaryVectorDataType) : base(binaryVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetItemsSpan(ReadOnlyMemory<TItem> data) => data.Span;

        private protected override ReadOnlyMemory<TItem> CreateResult(TItem[] items) => new(items);
    }
}
