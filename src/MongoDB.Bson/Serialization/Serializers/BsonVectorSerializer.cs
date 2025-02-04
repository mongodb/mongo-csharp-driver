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
using MongoDB.Bson.ObjectModel;

namespace MongoDB.Bson.Serialization.Serializers
{
    internal static class BsonVectorSerializer
    {
        public static BsonVectorSerializer<BsonVectorFloat32, float> BsonVectorFloat32Serializer { get; } = new BsonVectorSerializer<BsonVectorFloat32, float>(BsonVectorDataType.Float32);
        public static BsonVectorSerializer<BsonVectorInt8, byte> BsonVectorInt8Serializer { get; } = new BsonVectorSerializer<BsonVectorInt8, byte>(BsonVectorDataType.Int8);
        public static BsonVectorSerializer<BsonVectorPackedBit, byte> BsonVectorPackedBitSerializer { get; } = new BsonVectorSerializer<BsonVectorPackedBit, byte>(BsonVectorDataType.PackedBit);

        public static IBsonSerializer CreateArraySerializer(Type itemType, BsonVectorDataType bsonVectorDataType) =>
            CreateSerializerInstance(typeof(ArrayAsBsonVectorSerializer<>).MakeGenericType(itemType), bsonVectorDataType);

        public static IBsonSerializer CreateBsonVectorSerializer(Type bsonVectorType, Type itemType, BsonVectorDataType bsonVectorDataType) =>
            CreateSerializerInstance(typeof(BsonVectorSerializer<,>).MakeGenericType(bsonVectorType, itemType), bsonVectorDataType);

        public static IBsonSerializer CreateMemorySerializer(Type itemType, BsonVectorDataType bsonVectorDataType) =>
            CreateSerializerInstance(typeof(MemoryAsBsonVectorSerializer<>).MakeGenericType(itemType), bsonVectorDataType);

        public static IBsonSerializer CreateReadonlyMemorySerializer(Type itemType, BsonVectorDataType bsonVectorDataType) =>
            CreateSerializerInstance(typeof(ReadOnlyMemoryAsBsonVectorSerializer<>).MakeGenericType(itemType), bsonVectorDataType);

        public static IBsonSerializer CreateSerializer(Type type, BsonVectorDataType bsonVectorDataType)
        {
            // Arrays
            if (type.IsArray)
            {
                var itemType = type.GetElementType();
                return CreateArraySerializer(itemType, bsonVectorDataType);
            }

            // BsonVector
            if (type == typeof(BsonVectorFloat32) ||
                type == typeof(BsonVectorInt8) ||
                type == typeof(BsonVectorPackedBit))
            {
                return CreateBsonVectorSerializer(type, GetItemType(type.BaseType), bsonVectorDataType);
            }

            // Memory/ReadonlyMemory
            var genericTypeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (genericTypeDefinition == typeof(Memory<>))
            {
                return CreateMemorySerializer(GetItemType(type), bsonVectorDataType);
            }
            else if (genericTypeDefinition == typeof(ReadOnlyMemory<>))
            {
                return CreateReadonlyMemorySerializer(GetItemType(type), bsonVectorDataType);
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

        private static IBsonSerializer CreateSerializerInstance(Type serializerType, BsonVectorDataType bsonVectorDataType) =>
             (IBsonSerializer)Activator.CreateInstance(serializerType, bsonVectorDataType);
    }

    /// <summary>
    /// Represents a serializer for TItemContainer values represented as a BsonVector.
    /// </summary>
    /// <typeparam name="TItemContainer">The items container, for example <see cref="BsonVectorBase{TItem}"/> or <see cref="Memory{TItem}"/>.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public abstract class BsonVectorSerializerBase<TItemContainer, TItem> : SerializerBase<TItemContainer>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorSerializerBase{TItemContainer, TItem}"/> class.
        /// </summary>
        /// <param name="bsonVectorDataType">Type of the bson vector data.</param>
        private protected BsonVectorSerializerBase(BsonVectorDataType bsonVectorDataType)
        {
            BsonVectorReader.ValidateItemType<TItem>(bsonVectorDataType);

            VectorDataType = bsonVectorDataType;
        }

        /// <summary>
        /// Gets the type of the vector data.
        /// </summary>
        public BsonVectorDataType VectorDataType { get; }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is BsonVectorSerializerBase<TItemContainer, TItem> other &&
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
    /// Represents a serializer for <see cref="BsonVectorBase{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItemContainer">The concrete type derived from <see cref="BsonVectorBase{TItem}"/>.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class BsonVectorSerializer<TItemContainer, TItem> : BsonVectorSerializerBase<TItemContainer, TItem>
        where TItemContainer : BsonVectorBase<TItem>
        where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorSerializer{TItemContainer, TItem}" /> class.
        /// </summary>
        public BsonVectorSerializer(BsonVectorDataType bsonVectorDataType) :
            base(bsonVectorDataType)
        {
        }

        /// <inheritdoc/>
        public override TItemContainer Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var binaryData = ReadAndValidateBsonBinaryData(context.Reader);
            return (TItemContainer)binaryData.ToBsonVector<TItem>();
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TItemContainer value)
        {
            var binaryData = value.ToBsonBinaryData();

            context.Writer.WriteBinaryData(binaryData);
        }
    }

    /// <summary>
    /// A base class for serializers for <typeparamref name="TItem"/> containers represented as a BsonVector.
    /// </summary>
    /// <typeparam name="TItemContainer">The collection type.</typeparam>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public abstract class ItemContainerAsBsonVectorSerializer<TItemContainer, TItem> : BsonVectorSerializerBase<TItemContainer, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemContainerAsBsonVectorSerializer{TItemContainer, TItem}" /> class.
        /// </summary>
        protected ItemContainerAsBsonVectorSerializer(BsonVectorDataType bsonVectorDataType) :
            base(bsonVectorDataType)
        {
        }

        /// <inheritdoc/>
        public sealed override TItemContainer Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var binaryData = ReadAndValidateBsonBinaryData(context.Reader);
            var (items, padding, _) = binaryData.ToBsonVectorAsArray<TItem>();

            if (padding != 0)
            {
                throw new FormatException($"Non-zero padding is supported only in {nameof(BsonVectorPackedBit)} data type.");
            }

            return CreateResult(items);
        }

        /// <inheritdoc/>
        public sealed override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TItemContainer value)
        {
            var vectorData = GetItemsSpan(value);
            var bytes = BsonVectorWriter.WriteToBytes(vectorData, VectorDataType, 0);
            var binaryData = new BsonBinaryData(bytes, BsonBinarySubType.Vector);

            context.Writer.WriteBinaryData(binaryData);
        }

        private protected abstract TItemContainer CreateResult(TItem[] items);
        private protected abstract ReadOnlySpan<TItem> GetItemsSpan(TItemContainer data);
    }

    /// <summary>
    /// Represents a serializer for <typeparamref name="TItem"/> arrays represented as a BsonVector.
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class ArrayAsBsonVectorSerializer<TItem> : ItemContainerAsBsonVectorSerializer<TItem[], TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayAsBsonVectorSerializer{TItem}" /> class.
        /// </summary>
        public ArrayAsBsonVectorSerializer(BsonVectorDataType bsonVectorDataType) : base(bsonVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetItemsSpan(TItem[] data) => data;

        private protected override TItem[] CreateResult(TItem[] items) => items;
    }

    /// <summary>
    /// Represents a serializer for <see cref="Memory{TItem}"/> represented as a BsonVector.
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class MemoryAsBsonVectorSerializer<TItem> : ItemContainerAsBsonVectorSerializer<Memory<TItem>, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryAsBsonVectorSerializer{TItem}" /> class.
        /// </summary>
        public MemoryAsBsonVectorSerializer(BsonVectorDataType bsonVectorDataType) : base(bsonVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetItemsSpan(Memory<TItem> data) => data.Span;

        private protected override Memory<TItem> CreateResult(TItem[] items) => new(items);
    }

    /// <summary>
    /// Represents a serializer for <see cref="ReadOnlyMemory{TItem}"/> represented as a BsonVector.
    /// </summary>
    /// <typeparam name="TItem">The .NET data type.</typeparam>
    public sealed class ReadOnlyMemoryAsBsonVectorSerializer<TItem> : ItemContainerAsBsonVectorSerializer<ReadOnlyMemory<TItem>, TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyMemoryAsBsonVectorSerializer{TItem}" /> class.
        /// </summary>
        public ReadOnlyMemoryAsBsonVectorSerializer(BsonVectorDataType bsonVectorDataType) : base(bsonVectorDataType)
        {
        }

        private protected override ReadOnlySpan<TItem> GetItemsSpan(ReadOnlyMemory<TItem> data) => data.Span;

        private protected override ReadOnlyMemory<TItem> CreateResult(TItem[] items) => new(items);
    }
}
