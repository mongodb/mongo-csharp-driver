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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for <see cref="ReadOnlyMemory{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of the item. Only primitive numeric types are supported.</typeparam>
    public sealed class ReadonlyMemorySerializer<TItem> : MemorySerializerBase<TItem, ReadOnlyMemory<TItem>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadonlyMemorySerializer{TItem}" /> class.
        /// </summary>
        public ReadonlyMemorySerializer() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadonlyMemorySerializer{TItem}" /> class.
        /// </summary>
        public ReadonlyMemorySerializer(BsonType representation) : base(representation)
        {
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public override MemorySerializerBase<TItem, ReadOnlyMemory<TItem>> WithRepresentation(BsonType representation)
        {
            if (representation == Representation)
            {
                return this;
            }
            else
            {
                return new ReadonlyMemorySerializer<TItem>(representation);
            }
        }

        /// <inheritdoc/>
        protected override ReadOnlyMemory<TItem> CreateMemory(TItem[] items) => items;

        /// <inheritdoc/>
        protected override Memory<TItem> GetMemory(ReadOnlyMemory<TItem> memory) => MemoryMarshal.AsMemory(memory);
    }

    /// <summary>
    /// Represents a serializer for <see cref="Memory{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of the item. Only primitive numeric types are supported.</typeparam>
    public sealed class MemorySerializer<TItem> : MemorySerializerBase<TItem, Memory<TItem>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemorySerializer{TItem}" /> class.
        /// </summary>
        public MemorySerializer() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemorySerializer{TItem}" /> class.
        /// </summary>
        public MemorySerializer(BsonType representation) : base(representation)
        {
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public override MemorySerializerBase<TItem, Memory<TItem>> WithRepresentation(BsonType representation)
        {
            if (representation == Representation)
            {
                return this;
            }
            else
            {
                return new MemorySerializer<TItem>(representation);
            }
        }

        /// <inheritdoc/>
        protected override Memory<TItem> CreateMemory(TItem[] items) => items;

        /// <inheritdoc/>
        protected override Memory<TItem> GetMemory(Memory<TItem> memory) => memory;
    }

    /// <summary>
    /// Represents an abstract base class for <see cref="Memory{TItem}"/> and <see cref="ReadOnlyMemory{TItem}"/> serializers.
    /// </summary>
    /// <typeparam name="TItem">The type of the item. Only primitive numeric types are supported.</typeparam>
    /// <typeparam name="TMemory">The type of the memory struct.</typeparam>
    public abstract class MemorySerializerBase<TItem, TMemory> : StructSerializerBase<TMemory>, IRepresentationConfigurable<MemorySerializerBase<TItem, TMemory>>
        where TMemory : struct
    {
        private static readonly bool __isByte = (typeof(TItem) == typeof(byte));

        private readonly Func<IBsonReader, TItem[]> _readItems;
        private readonly BsonType _representation;
        private readonly Action<IBsonWriter, Memory<TItem>> _writeItems;

        /// <inheritdoc/>
        public BsonType Representation => _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MemorySerializerBase{TItem, TMemory}" /> class.
        /// </summary>
        public MemorySerializerBase(BsonType representation)
        {
            if (representation != BsonType.Array &&
                !(__isByte && representation == BsonType.Binary))
            {
                throw new ArgumentOutOfRangeException(nameof(representation));
            }

            (_readItems, _writeItems) = GetReaderAndWriter();
            _representation = representation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemorySerializerBase{TItem, TMemory}" /> class.
        /// </summary>
        public MemorySerializerBase() :
            this(__isByte ? BsonType.Binary : BsonType.Array) // Match the serialization behavior for arrays
        {
        }

        // public methods
        /// <inheritdoc/>
        public override TMemory Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            var bsonType = reader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Array:
                    var items = _readItems(reader);

                    return CreateMemory(items);
                case BsonType.Binary:
                    if (!__isByte)
                    {
                        throw CreateCannotDeserializeFromBsonTypeException(bsonType);
                    }
                    var bytes = reader.ReadBytes();
                    return CreateMemory(bytes as TItem[]);
                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is MemorySerializerBase<TItem, TMemory> other &&
                _representation.Equals(other._representation);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TMemory value)
        {
            var memory = GetMemory(value);

            switch (Representation)
            {
                case BsonType.Array:
                    _writeItems(context.Writer, memory);
                    break;
                case BsonType.Binary:
                    var bytesMemory = Unsafe.As<Memory<TItem>, Memory<byte>>(ref memory);
                    var bytes = MemoryMarshal.AsBytes(bytesMemory.Span);
                    context.Writer.WriteBytes(bytes.ToArray());
                    break;
                default:
                    throw new NotSupportedException(nameof(Representation));
            }
        }

        /// <inheritdoc/>
        public abstract MemorySerializerBase<TItem, TMemory> WithRepresentation(BsonType representation);

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation) =>
            WithRepresentation(representation);

        /// <summary>
        /// Creates the Memory{TITem} structure.
        /// </summary>
        /// <param name="items">The items to initialize the resulting instance with.</param>
        /// <returns>The created memory structure.</returns>
        protected abstract TMemory CreateMemory(TItem[] items);

        /// <summary>
        /// Get the memory structure from TMemory instance.
        /// </summary>
        /// <returns>The Memory{TITem} structure.</returns>
        protected abstract Memory<TItem> GetMemory(TMemory memory);

        private static (Func<IBsonReader, TItem[]> reader, Action<IBsonWriter, Memory<TItem>> writer) GetReaderAndWriter()
        {
            Func<IBsonReader, TItem[]> readItems;
            Action<IBsonWriter, Memory<TItem>> writeItems;

            switch (typeof(TItem))
            {
                case var t when t == typeof(bool):
                    readItems = reader => PrimitivesArrayReader.ReadBool(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<bool>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteBool(writer, span);
                    };
                    break;
                case var t when t == typeof(sbyte):
                    readItems = reader => PrimitivesArrayReader.ReadInt8(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<sbyte>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteInt8(writer, span);
                    };
                    break;
                case var t when t == typeof(byte):
                    readItems = reader => PrimitivesArrayReader.ReadUInt8(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<byte>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteUInt8(writer, span);
                    };
                    break;
                case var t when t == typeof(char):
                    readItems = reader => PrimitivesArrayReader.ReadChar(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<char>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteChar(writer, span);
                    };
                    break;
                case var t when t == typeof(short):
                    readItems = reader => PrimitivesArrayReader.ReadInt16(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<short>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteInt16(writer, span);
                    };
                    break;
                case var t when t == typeof(ushort):
                    readItems = reader => PrimitivesArrayReader.ReadUInt16(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<ushort>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteUInt16(writer, span);
                    };
                    break;
                case var t when t == typeof(int):
                    readItems = reader => PrimitivesArrayReader.ReadInt32(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<int>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteInt32(writer, span);
                    };
                    break;
                case var t when t == typeof(uint):
                    readItems = reader => PrimitivesArrayReader.ReadUInt32(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<uint>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteUInt32(writer, span);
                    };
                    break;
                case var t when t == typeof(long):
                    readItems = reader => PrimitivesArrayReader.ReadInt64(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<long>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteInt64(writer, span);
                    };
                    break;
                case var t when t == typeof(ulong):
                    readItems = reader => PrimitivesArrayReader.ReadUInt64(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<ulong>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteUInt64(writer, span);
                    };
                    break;
                case var t when t == typeof(float):
                    readItems = reader => PrimitivesArrayReader.ReadSingles(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<float>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteSingles(writer, span);
                    };
                    break;
                case var t when t == typeof(double):
                    readItems = reader => PrimitivesArrayReader.ReadDoubles(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<double>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteDoubles(writer, span);
                    };
                    break;
                case var t when t == typeof(decimal):
                    readItems = reader => PrimitivesArrayReader.ReadDecimal128(reader) as TItem[];
                    writeItems = (writer, memory) =>
                    {
                        var span = Unsafe.As<Memory<TItem>, Memory<decimal>>(ref memory).Span;
                        PrimitivesArrayWriter.WriteDecimal128(writer, span);
                    };
                    break;
                default:
                    throw new NotSupportedException($"Not supported memory type {typeof(TItem)}. Only primitive numeric types are supported.");
            };

            return (readItems, writeItems);
        }
    }
}
