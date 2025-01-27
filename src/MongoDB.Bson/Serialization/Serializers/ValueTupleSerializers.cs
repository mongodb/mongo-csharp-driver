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
using System.Linq;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// A factory class for ValueTupleSerializers.
    /// </summary>
    public static class ValueTupleSerializer
    {
        /// <summary>
        /// Creates a ValueTupleSerializer.
        /// </summary>
        /// <param name="itemSerializers">The item serializers.</param>
        /// <returns>A ValueTupleSerializer.</returns>
        public static IBsonSerializer Create(IEnumerable<IBsonSerializer> itemSerializers)
        {
            var itemSerializersArray = itemSerializers.ToArray();
            var valueTupleSerializerType = CreateValueTupleSerializerType(itemSerializersArray);
            return (IBsonSerializer)Activator.CreateInstance(valueTupleSerializerType, itemSerializersArray);

            static Type CreateValueTupleSerializerType(IBsonSerializer[] itemSerializersArray)
            {
                var itemTypes = itemSerializersArray.Select(s => s.ValueType).ToArray();
                var valueTupleSerializerTypeDefinition = CreateValueTupleSerializerTypeDefinition(itemTypes.Length);
                return valueTupleSerializerTypeDefinition.MakeGenericType(itemTypes);
            }

            static Type CreateValueTupleSerializerTypeDefinition(int itemCount)
            {
                return itemCount switch
                {
                    1 => typeof(ValueTupleSerializer<>),
                    2 => typeof(ValueTupleSerializer<,>),
                    3 => typeof(ValueTupleSerializer<,,>),
                    4 => typeof(ValueTupleSerializer<,,,>),
                    5 => typeof(ValueTupleSerializer<,,,,>),
                    6 => typeof(ValueTupleSerializer<,,,,,>),
                    7 => typeof(ValueTupleSerializer<,,,,,,>),
                    8 => typeof(ValueTupleSerializer<,,,,,,,>),
                    _ => throw new Exception($"Invalid number of ValueTuple items : {itemCount}.")
                };
            }
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="ValueTuple{T1}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    public sealed class ValueTupleSerializer<T1> : StructSerializerBase<ValueTuple<T1>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1}"/> class.
        /// </summary>
        public ValueTupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        public ValueTupleSerializer(
            IBsonSerializer<T1> item1Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public ValueTupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException(nameof(serializerRegistry)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => serializerRegistry.GetSerializer<T1>());
        }

        // public properties
        /// <summary>
        /// Gets the Item1 serializer.
        /// </summary>
        public IBsonSerializer<T1> Item1Serializer => _lazyItem1Serializer.Value;

        // public methods
        /// <inheritdoc/>
        public override ValueTuple<T1> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            T1 item1 = default;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Array:
                    reader.ReadStartArray();
                    item1 = _lazyItem1Serializer.Value.Deserialize(context);
                    reader.ReadEndArray();
                    break;

                case BsonType.Document:
                    reader.ReadStartDocument();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = reader.ReadName();
                        switch (name)
                        {
                            case "Item1": item1 = _lazyItem1Serializer.Value.Deserialize(context); break;
                            default: throw new BsonSerializationException($"Invalid element {name} found while deserializing a ValueTuple.");
                        }
                    }
                    reader.ReadEndDocument();
                    break;

                default:
                    throw new BsonSerializationException($"Cannot deserialize a ValueTuple when BsonType is: {reader.CurrentBsonType}.");
            }

            return new ValueTuple<T1>(item1);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueTupleSerializer<T1> other &&
                object.ReferenceEquals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public IBsonSerializer GetItemSerializer(int itemNumber)
        {
            return itemNumber switch
            {
                1 => _lazyItem1Serializer.Value,
                _ => throw new IndexOutOfRangeException(nameof(itemNumber))
            };
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ValueTuple<T1> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="ValueTuple{T1, T2}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    public sealed class ValueTupleSerializer<T1, T2> : StructSerializerBase<ValueTuple<T1, T2>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2}"/> class.
        /// </summary>
        public ValueTupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        public ValueTupleSerializer(
            IBsonSerializer<T1> item1Serializer,
            IBsonSerializer<T2> item2Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }
            if (item2Serializer == null) { throw new ArgumentNullException(nameof(item2Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => item2Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public ValueTupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException(nameof(serializerRegistry)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => serializerRegistry.GetSerializer<T1>());
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => serializerRegistry.GetSerializer<T2>());
        }

        // public properties
        /// <summary>
        /// Gets the Item1 serializer.
        /// </summary>
        public IBsonSerializer<T1> Item1Serializer => _lazyItem1Serializer.Value;

        /// <summary>
        /// Gets the Item2 serializer.
        /// </summary>
        public IBsonSerializer<T2> Item2Serializer => _lazyItem2Serializer.Value;

        // public methods
        /// <inheritdoc/>
        public override ValueTuple<T1, T2> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            T1 item1 = default;
            T2 item2 = default;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Array:
                    reader.ReadStartArray();
                    item1 = _lazyItem1Serializer.Value.Deserialize(context);
                    item2 = _lazyItem2Serializer.Value.Deserialize(context);
                    reader.ReadEndArray();
                    break;

                case BsonType.Document:
                    reader.ReadStartDocument();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = reader.ReadName();
                        switch (name)
                        {
                            case "Item1": item1 = _lazyItem1Serializer.Value.Deserialize(context); break;
                            case "Item2": item2 = _lazyItem2Serializer.Value.Deserialize(context); break;
                            default: throw new BsonSerializationException($"Invalid element {name} found while deserializing a ValueTuple.");
                        }
                    }
                    reader.ReadEndDocument();
                    break;

                default:
                    throw new BsonSerializationException($"Cannot deserialize a ValueTuple when BsonType is: {reader.CurrentBsonType}.");
            }

            return new ValueTuple<T1, T2>(item1, item2);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueTupleSerializer<T1, T2> other &&
                object.ReferenceEquals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.ReferenceEquals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public IBsonSerializer GetItemSerializer(int itemNumber)
        {
            return itemNumber switch
            {
                1 => _lazyItem1Serializer.Value,
                2 => _lazyItem2Serializer.Value,
                _ => throw new IndexOutOfRangeException(nameof(itemNumber))
            };
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ValueTuple<T1, T2> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="ValueTuple{T1, T2, T3}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    public sealed class ValueTupleSerializer<T1, T2, T3> : StructSerializerBase<ValueTuple<T1, T2, T3>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3}"/> class.
        /// </summary>
        public ValueTupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        public ValueTupleSerializer(
            IBsonSerializer<T1> item1Serializer,
            IBsonSerializer<T2> item2Serializer,
            IBsonSerializer<T3> item3Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }
            if (item2Serializer == null) { throw new ArgumentNullException(nameof(item2Serializer)); }
            if (item3Serializer == null) { throw new ArgumentNullException(nameof(item3Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => item2Serializer);
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => item3Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public ValueTupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException(nameof(serializerRegistry)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => serializerRegistry.GetSerializer<T1>());
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => serializerRegistry.GetSerializer<T2>());
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => serializerRegistry.GetSerializer<T3>());
        }

        // public properties
        /// <summary>
        /// Gets the Item1 serializer.
        /// </summary>
        public IBsonSerializer<T1> Item1Serializer => _lazyItem1Serializer.Value;

        /// <summary>
        /// Gets the Item2 serializer.
        /// </summary>
        public IBsonSerializer<T2> Item2Serializer => _lazyItem2Serializer.Value;

        /// <summary>
        /// Gets the Item3 serializer.
        /// </summary>
        public IBsonSerializer<T3> Item3Serializer => _lazyItem3Serializer.Value;

        // public methods
        /// <inheritdoc/>
        public override ValueTuple<T1, T2, T3> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            T1 item1 = default;
            T2 item2 = default;
            T3 item3 = default;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Array:
                    reader.ReadStartArray();
                    item1 = _lazyItem1Serializer.Value.Deserialize(context);
                    item2 = _lazyItem2Serializer.Value.Deserialize(context);
                    item3 = _lazyItem3Serializer.Value.Deserialize(context);
                    reader.ReadEndArray();
                    break;

                case BsonType.Document:
                    reader.ReadStartDocument();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = reader.ReadName();
                        switch (name)
                        {
                            case "Item1": item1 = _lazyItem1Serializer.Value.Deserialize(context); break;
                            case "Item2": item2 = _lazyItem2Serializer.Value.Deserialize(context); break;
                            case "Item3": item3 = _lazyItem3Serializer.Value.Deserialize(context); break;
                            default: throw new BsonSerializationException($"Invalid element {name} found while deserializing a ValueTuple.");
                        }
                    }
                    reader.ReadEndDocument();
                    break;

                default:
                    throw new BsonSerializationException($"Cannot deserialize a ValueTuple when BsonType is: {reader.CurrentBsonType}.");
            }

            return new ValueTuple<T1, T2, T3>(item1, item2, item3);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueTupleSerializer<T1, T2, T3> other &&
                object.ReferenceEquals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.ReferenceEquals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.ReferenceEquals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public IBsonSerializer GetItemSerializer(int itemNumber)
        {
            return itemNumber switch
            {
                1 => _lazyItem1Serializer.Value,
                2 => _lazyItem2Serializer.Value,
                3 => _lazyItem3Serializer.Value,
                _ => throw new IndexOutOfRangeException(nameof(itemNumber))
            };
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ValueTuple<T1, T2, T3> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            _lazyItem3Serializer.Value.Serialize(context, value.Item3);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="ValueTuple{T1, T2, T3, T4}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    public sealed class ValueTupleSerializer<T1, T2, T3, T4> : StructSerializerBase<ValueTuple<T1, T2, T3, T4>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;
        private readonly Lazy<IBsonSerializer<T4>> _lazyItem4Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4}"/> class.
        /// </summary>
        public ValueTupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        public ValueTupleSerializer(
            IBsonSerializer<T1> item1Serializer,
            IBsonSerializer<T2> item2Serializer,
            IBsonSerializer<T3> item3Serializer,
            IBsonSerializer<T4> item4Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }
            if (item2Serializer == null) { throw new ArgumentNullException(nameof(item2Serializer)); }
            if (item3Serializer == null) { throw new ArgumentNullException(nameof(item3Serializer)); }
            if (item4Serializer == null) { throw new ArgumentNullException(nameof(item4Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => item2Serializer);
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => item3Serializer);
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => item4Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public ValueTupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException(nameof(serializerRegistry)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => serializerRegistry.GetSerializer<T1>());
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => serializerRegistry.GetSerializer<T2>());
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => serializerRegistry.GetSerializer<T3>());
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => serializerRegistry.GetSerializer<T4>());
        }

        // public properties
        /// <summary>
        /// Gets the Item1 serializer.
        /// </summary>
        public IBsonSerializer<T1> Item1Serializer => _lazyItem1Serializer.Value;

        /// <summary>
        /// Gets the Item2 serializer.
        /// </summary>
        public IBsonSerializer<T2> Item2Serializer => _lazyItem2Serializer.Value;

        /// <summary>
        /// Gets the Item3 serializer.
        /// </summary>
        public IBsonSerializer<T3> Item3Serializer => _lazyItem3Serializer.Value;

        /// <summary>
        /// Gets the Item4 serializer.
        /// </summary>
        public IBsonSerializer<T4> Item4Serializer => _lazyItem4Serializer.Value;

        // public methods
        /// <inheritdoc/>
        public override ValueTuple<T1, T2, T3, T4> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            T1 item1 = default;
            T2 item2 = default;
            T3 item3 = default;
            T4 item4 = default;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Array:
                    reader.ReadStartArray();
                    item1 = _lazyItem1Serializer.Value.Deserialize(context);
                    item2 = _lazyItem2Serializer.Value.Deserialize(context);
                    item3 = _lazyItem3Serializer.Value.Deserialize(context);
                    item4 = _lazyItem4Serializer.Value.Deserialize(context);
                    reader.ReadEndArray();
                    break;

                case BsonType.Document:
                    reader.ReadStartDocument();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = reader.ReadName();
                        switch (name)
                        {
                            case "Item1": item1 = _lazyItem1Serializer.Value.Deserialize(context); break;
                            case "Item2": item2 = _lazyItem2Serializer.Value.Deserialize(context); break;
                            case "Item3": item3 = _lazyItem3Serializer.Value.Deserialize(context); break;
                            case "Item4": item4 = _lazyItem4Serializer.Value.Deserialize(context); break;
                            default: throw new BsonSerializationException($"Invalid element {name} found while deserializing a ValueTuple.");
                        }
                    }
                    reader.ReadEndDocument();
                    break;

                default:
                    throw new BsonSerializationException($"Cannot deserialize a ValueTuple when BsonType is: {reader.CurrentBsonType}.");
            }

            return new ValueTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueTupleSerializer<T1, T2, T3, T4> other &&
                object.ReferenceEquals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.ReferenceEquals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.ReferenceEquals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.ReferenceEquals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public IBsonSerializer GetItemSerializer(int itemNumber)
        {
            return itemNumber switch
            {
                1 => _lazyItem1Serializer.Value,
                2 => _lazyItem2Serializer.Value,
                3 => _lazyItem3Serializer.Value,
                4 => _lazyItem4Serializer.Value,
                _ => throw new IndexOutOfRangeException(nameof(itemNumber))
            };
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ValueTuple<T1, T2, T3, T4> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            _lazyItem3Serializer.Value.Serialize(context, value.Item3);
            _lazyItem4Serializer.Value.Serialize(context, value.Item4);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="ValueTuple{T1, T2, T3, T4, T5}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    /// <typeparam name="T5">The type of item 5.</typeparam>
    public sealed class ValueTupleSerializer<T1, T2, T3, T4, T5> : StructSerializerBase<ValueTuple<T1, T2, T3, T4, T5>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;
        private readonly Lazy<IBsonSerializer<T4>> _lazyItem4Serializer;
        private readonly Lazy<IBsonSerializer<T5>> _lazyItem5Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        public ValueTupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        /// <param name="item5Serializer">The Item5 serializer.</param>
        public ValueTupleSerializer(
            IBsonSerializer<T1> item1Serializer,
            IBsonSerializer<T2> item2Serializer,
            IBsonSerializer<T3> item3Serializer,
            IBsonSerializer<T4> item4Serializer,
            IBsonSerializer<T5> item5Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }
            if (item2Serializer == null) { throw new ArgumentNullException(nameof(item2Serializer)); }
            if (item3Serializer == null) { throw new ArgumentNullException(nameof(item3Serializer)); }
            if (item4Serializer == null) { throw new ArgumentNullException(nameof(item4Serializer)); }
            if (item5Serializer == null) { throw new ArgumentNullException(nameof(item5Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => item2Serializer);
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => item3Serializer);
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => item4Serializer);
            _lazyItem5Serializer = new Lazy<IBsonSerializer<T5>>(() => item5Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public ValueTupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException(nameof(serializerRegistry)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => serializerRegistry.GetSerializer<T1>());
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => serializerRegistry.GetSerializer<T2>());
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => serializerRegistry.GetSerializer<T3>());
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => serializerRegistry.GetSerializer<T4>());
            _lazyItem5Serializer = new Lazy<IBsonSerializer<T5>>(() => serializerRegistry.GetSerializer<T5>());
        }

        // public properties
        /// <summary>
        /// Gets the Item1 serializer.
        /// </summary>
        public IBsonSerializer<T1> Item1Serializer => _lazyItem1Serializer.Value;

        /// <summary>
        /// Gets the Item2 serializer.
        /// </summary>
        public IBsonSerializer<T2> Item2Serializer => _lazyItem2Serializer.Value;

        /// <summary>
        /// Gets the Item3 serializer.
        /// </summary>
        public IBsonSerializer<T3> Item3Serializer => _lazyItem3Serializer.Value;

        /// <summary>
        /// Gets the Item4 serializer.
        /// </summary>
        public IBsonSerializer<T4> Item4Serializer => _lazyItem4Serializer.Value;

        /// <summary>
        /// Gets the Item5 serializer.
        /// </summary>
        public IBsonSerializer<T5> Item5Serializer => _lazyItem5Serializer.Value;

        // public methods
        /// <inheritdoc/>
        public override ValueTuple<T1, T2, T3, T4, T5> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            T1 item1 = default;
            T2 item2 = default;
            T3 item3 = default;
            T4 item4 = default;
            T5 item5 = default;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Array:
                    reader.ReadStartArray();
                    item1 = _lazyItem1Serializer.Value.Deserialize(context);
                    item2 = _lazyItem2Serializer.Value.Deserialize(context);
                    item3 = _lazyItem3Serializer.Value.Deserialize(context);
                    item4 = _lazyItem4Serializer.Value.Deserialize(context);
                    item5 = _lazyItem5Serializer.Value.Deserialize(context);
                    reader.ReadEndArray();
                    break;

                case BsonType.Document:
                    reader.ReadStartDocument();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = reader.ReadName();
                        switch (name)
                        {
                            case "Item1": item1 = _lazyItem1Serializer.Value.Deserialize(context); break;
                            case "Item2": item2 = _lazyItem2Serializer.Value.Deserialize(context); break;
                            case "Item3": item3 = _lazyItem3Serializer.Value.Deserialize(context); break;
                            case "Item4": item4 = _lazyItem4Serializer.Value.Deserialize(context); break;
                            case "Item5": item5 = _lazyItem5Serializer.Value.Deserialize(context); break;
                            default: throw new BsonSerializationException($"Invalid element {name} found while deserializing a ValueTuple.");
                        }
                    }
                    reader.ReadEndDocument();
                    break;

                default:
                    throw new BsonSerializationException($"Cannot deserialize a ValueTuple when BsonType is: {reader.CurrentBsonType}.");
            }

            return new ValueTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueTupleSerializer<T1, T2, T3, T4, T5> other &&
                object.ReferenceEquals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.ReferenceEquals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.ReferenceEquals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.ReferenceEquals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value) &&
                object.ReferenceEquals(_lazyItem5Serializer.Value, other._lazyItem5Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public IBsonSerializer GetItemSerializer(int itemNumber)
        {
            return itemNumber switch
            {
                1 => _lazyItem1Serializer.Value,
                2 => _lazyItem2Serializer.Value,
                3 => _lazyItem3Serializer.Value,
                4 => _lazyItem4Serializer.Value,
                5 => _lazyItem5Serializer.Value,
                _ => throw new IndexOutOfRangeException(nameof(itemNumber))
            };
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ValueTuple<T1, T2, T3, T4, T5> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            _lazyItem3Serializer.Value.Serialize(context, value.Item3);
            _lazyItem4Serializer.Value.Serialize(context, value.Item4);
            _lazyItem5Serializer.Value.Serialize(context, value.Item5);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    /// <typeparam name="T5">The type of item 5.</typeparam>
    /// <typeparam name="T6">The type of item 6.</typeparam>
    public sealed class ValueTupleSerializer<T1, T2, T3, T4, T5, T6> : StructSerializerBase<ValueTuple<T1, T2, T3, T4, T5, T6>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;
        private readonly Lazy<IBsonSerializer<T4>> _lazyItem4Serializer;
        private readonly Lazy<IBsonSerializer<T5>> _lazyItem5Serializer;
        private readonly Lazy<IBsonSerializer<T6>> _lazyItem6Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        public ValueTupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        /// <param name="item5Serializer">The Item5 serializer.</param>
        /// <param name="item6Serializer">The Item6 serializer.</param>
        public ValueTupleSerializer(
            IBsonSerializer<T1> item1Serializer,
            IBsonSerializer<T2> item2Serializer,
            IBsonSerializer<T3> item3Serializer,
            IBsonSerializer<T4> item4Serializer,
            IBsonSerializer<T5> item5Serializer,
            IBsonSerializer<T6> item6Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }
            if (item2Serializer == null) { throw new ArgumentNullException(nameof(item2Serializer)); }
            if (item3Serializer == null) { throw new ArgumentNullException(nameof(item3Serializer)); }
            if (item4Serializer == null) { throw new ArgumentNullException(nameof(item4Serializer)); }
            if (item5Serializer == null) { throw new ArgumentNullException(nameof(item5Serializer)); }
            if (item6Serializer == null) { throw new ArgumentNullException(nameof(item6Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => item2Serializer);
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => item3Serializer);
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => item4Serializer);
            _lazyItem5Serializer = new Lazy<IBsonSerializer<T5>>(() => item5Serializer);
            _lazyItem6Serializer = new Lazy<IBsonSerializer<T6>>(() => item6Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public ValueTupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException(nameof(serializerRegistry)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => serializerRegistry.GetSerializer<T1>());
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => serializerRegistry.GetSerializer<T2>());
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => serializerRegistry.GetSerializer<T3>());
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => serializerRegistry.GetSerializer<T4>());
            _lazyItem5Serializer = new Lazy<IBsonSerializer<T5>>(() => serializerRegistry.GetSerializer<T5>());
            _lazyItem6Serializer = new Lazy<IBsonSerializer<T6>>(() => serializerRegistry.GetSerializer<T6>());
        }

        // public properties
        /// <summary>
        /// Gets the Item1 serializer.
        /// </summary>
        public IBsonSerializer<T1> Item1Serializer => _lazyItem1Serializer.Value;

        /// <summary>
        /// Gets the Item2 serializer.
        /// </summary>
        public IBsonSerializer<T2> Item2Serializer => _lazyItem2Serializer.Value;

        /// <summary>
        /// Gets the Item3 serializer.
        /// </summary>
        public IBsonSerializer<T3> Item3Serializer => _lazyItem3Serializer.Value;

        /// <summary>
        /// Gets the Item4 serializer.
        /// </summary>
        public IBsonSerializer<T4> Item4Serializer => _lazyItem4Serializer.Value;

        /// <summary>
        /// Gets the Item5 serializer.
        /// </summary>
        public IBsonSerializer<T5> Item5Serializer => _lazyItem5Serializer.Value;

        /// <summary>
        /// Gets the Item6 serializer.
        /// </summary>
        public IBsonSerializer<T6> Item6Serializer => _lazyItem6Serializer.Value;

        // public methods
        /// <inheritdoc/>
        public override ValueTuple<T1, T2, T3, T4, T5, T6> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            T1 item1 = default;
            T2 item2 = default;
            T3 item3 = default;
            T4 item4 = default;
            T5 item5 = default;
            T6 item6 = default;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Array:
                    reader.ReadStartArray();
                    item1 = _lazyItem1Serializer.Value.Deserialize(context);
                    item2 = _lazyItem2Serializer.Value.Deserialize(context);
                    item3 = _lazyItem3Serializer.Value.Deserialize(context);
                    item4 = _lazyItem4Serializer.Value.Deserialize(context);
                    item5 = _lazyItem5Serializer.Value.Deserialize(context);
                    item6 = _lazyItem6Serializer.Value.Deserialize(context);
                    reader.ReadEndArray();
                    break;

                case BsonType.Document:
                    reader.ReadStartDocument();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = reader.ReadName();
                        switch (name)
                        {
                            case "Item1": item1 = _lazyItem1Serializer.Value.Deserialize(context); break;
                            case "Item2": item2 = _lazyItem2Serializer.Value.Deserialize(context); break;
                            case "Item3": item3 = _lazyItem3Serializer.Value.Deserialize(context); break;
                            case "Item4": item4 = _lazyItem4Serializer.Value.Deserialize(context); break;
                            case "Item5": item5 = _lazyItem5Serializer.Value.Deserialize(context); break;
                            case "Item6": item6 = _lazyItem6Serializer.Value.Deserialize(context); break;
                            default: throw new BsonSerializationException($"Invalid element {name} found while deserializing a ValueTuple.");
                        }
                    }
                    reader.ReadEndDocument();
                    break;

                default:
                    throw new BsonSerializationException($"Cannot deserialize a ValueTuple when BsonType is: {reader.CurrentBsonType}.");
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueTupleSerializer<T1, T2, T3, T4, T5, T6> other &&
                object.ReferenceEquals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.ReferenceEquals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.ReferenceEquals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.ReferenceEquals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value) &&
                object.ReferenceEquals(_lazyItem5Serializer.Value, other._lazyItem5Serializer.Value) &&
                object.ReferenceEquals(_lazyItem6Serializer.Value, other._lazyItem6Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public IBsonSerializer GetItemSerializer(int itemNumber)
        {
            return itemNumber switch
            {
                1 => _lazyItem1Serializer.Value,
                2 => _lazyItem2Serializer.Value,
                3 => _lazyItem3Serializer.Value,
                4 => _lazyItem4Serializer.Value,
                5 => _lazyItem5Serializer.Value,
                6 => _lazyItem6Serializer.Value,
                _ => throw new IndexOutOfRangeException(nameof(itemNumber))
            };
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ValueTuple<T1, T2, T3, T4, T5, T6> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            _lazyItem3Serializer.Value.Serialize(context, value.Item3);
            _lazyItem4Serializer.Value.Serialize(context, value.Item4);
            _lazyItem5Serializer.Value.Serialize(context, value.Item5);
            _lazyItem6Serializer.Value.Serialize(context, value.Item6);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    /// <typeparam name="T5">The type of item 5.</typeparam>
    /// <typeparam name="T6">The type of item 6.</typeparam>
    /// <typeparam name="T7">The type of item 7.</typeparam>
    public sealed class ValueTupleSerializer<T1, T2, T3, T4, T5, T6, T7> : StructSerializerBase<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;
        private readonly Lazy<IBsonSerializer<T4>> _lazyItem4Serializer;
        private readonly Lazy<IBsonSerializer<T5>> _lazyItem5Serializer;
        private readonly Lazy<IBsonSerializer<T6>> _lazyItem6Serializer;
        private readonly Lazy<IBsonSerializer<T7>> _lazyItem7Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        public ValueTupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        /// <param name="item5Serializer">The Item5 serializer.</param>
        /// <param name="item6Serializer">The Item6 serializer.</param>
        /// <param name="item7Serializer">The Item7 serializer.</param>
        public ValueTupleSerializer(
            IBsonSerializer<T1> item1Serializer,
            IBsonSerializer<T2> item2Serializer,
            IBsonSerializer<T3> item3Serializer,
            IBsonSerializer<T4> item4Serializer,
            IBsonSerializer<T5> item5Serializer,
            IBsonSerializer<T6> item6Serializer,
            IBsonSerializer<T7> item7Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }
            if (item2Serializer == null) { throw new ArgumentNullException(nameof(item2Serializer)); }
            if (item3Serializer == null) { throw new ArgumentNullException(nameof(item3Serializer)); }
            if (item4Serializer == null) { throw new ArgumentNullException(nameof(item4Serializer)); }
            if (item5Serializer == null) { throw new ArgumentNullException(nameof(item5Serializer)); }
            if (item6Serializer == null) { throw new ArgumentNullException(nameof(item6Serializer)); }
            if (item7Serializer == null) { throw new ArgumentNullException(nameof(item7Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => item2Serializer);
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => item3Serializer);
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => item4Serializer);
            _lazyItem5Serializer = new Lazy<IBsonSerializer<T5>>(() => item5Serializer);
            _lazyItem6Serializer = new Lazy<IBsonSerializer<T6>>(() => item6Serializer);
            _lazyItem7Serializer = new Lazy<IBsonSerializer<T7>>(() => item7Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6, T7}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public ValueTupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException(nameof(serializerRegistry)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => serializerRegistry.GetSerializer<T1>());
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => serializerRegistry.GetSerializer<T2>());
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => serializerRegistry.GetSerializer<T3>());
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => serializerRegistry.GetSerializer<T4>());
            _lazyItem5Serializer = new Lazy<IBsonSerializer<T5>>(() => serializerRegistry.GetSerializer<T5>());
            _lazyItem6Serializer = new Lazy<IBsonSerializer<T6>>(() => serializerRegistry.GetSerializer<T6>());
            _lazyItem7Serializer = new Lazy<IBsonSerializer<T7>>(() => serializerRegistry.GetSerializer<T7>());
        }

        // public properties
        /// <summary>
        /// Gets the Item1 serializer.
        /// </summary>
        public IBsonSerializer<T1> Item1Serializer => _lazyItem1Serializer.Value;

        /// <summary>
        /// Gets the Item2 serializer.
        /// </summary>
        public IBsonSerializer<T2> Item2Serializer => _lazyItem2Serializer.Value;

        /// <summary>
        /// Gets the Item3 serializer.
        /// </summary>
        public IBsonSerializer<T3> Item3Serializer => _lazyItem3Serializer.Value;

        /// <summary>
        /// Gets the Item4 serializer.
        /// </summary>
        public IBsonSerializer<T4> Item4Serializer => _lazyItem4Serializer.Value;

        /// <summary>
        /// Gets the Item5 serializer.
        /// </summary>
        public IBsonSerializer<T5> Item5Serializer => _lazyItem5Serializer.Value;

        /// <summary>
        /// Gets the Item6 serializer.
        /// </summary>
        public IBsonSerializer<T6> Item6Serializer => _lazyItem6Serializer.Value;

        /// <summary>
        /// Gets the Item7 serializer.
        /// </summary>
        public IBsonSerializer<T7> Item7Serializer => _lazyItem7Serializer.Value;

        // public methods
        /// <inheritdoc/>
        public override ValueTuple<T1, T2, T3, T4, T5, T6, T7> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            T1 item1 = default;
            T2 item2 = default;
            T3 item3 = default;
            T4 item4 = default;
            T5 item5 = default;
            T6 item6 = default;
            T7 item7 = default;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Array:
                    reader.ReadStartArray();
                    item1 = _lazyItem1Serializer.Value.Deserialize(context);
                    item2 = _lazyItem2Serializer.Value.Deserialize(context);
                    item3 = _lazyItem3Serializer.Value.Deserialize(context);
                    item4 = _lazyItem4Serializer.Value.Deserialize(context);
                    item5 = _lazyItem5Serializer.Value.Deserialize(context);
                    item6 = _lazyItem6Serializer.Value.Deserialize(context);
                    item7 = _lazyItem7Serializer.Value.Deserialize(context);
                    reader.ReadEndArray();
                    break;

                case BsonType.Document:
                    reader.ReadStartDocument();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = reader.ReadName();
                        switch (name)
                        {
                            case "Item1": item1 = _lazyItem1Serializer.Value.Deserialize(context); break;
                            case "Item2": item2 = _lazyItem2Serializer.Value.Deserialize(context); break;
                            case "Item3": item3 = _lazyItem3Serializer.Value.Deserialize(context); break;
                            case "Item4": item4 = _lazyItem4Serializer.Value.Deserialize(context); break;
                            case "Item5": item5 = _lazyItem5Serializer.Value.Deserialize(context); break;
                            case "Item6": item6 = _lazyItem6Serializer.Value.Deserialize(context); break;
                            case "Item7": item7 = _lazyItem7Serializer.Value.Deserialize(context); break;
                            default: throw new BsonSerializationException($"Invalid element {name} found while deserializing a ValueTuple.");
                        }
                    }
                    reader.ReadEndDocument();
                    break;

                default:
                    throw new BsonSerializationException($"Cannot deserialize a ValueTuple when BsonType is: {reader.CurrentBsonType}.");
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueTupleSerializer<T1, T2, T3, T4, T5, T6, T7> other &&
                object.ReferenceEquals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.ReferenceEquals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.ReferenceEquals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.ReferenceEquals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value) &&
                object.ReferenceEquals(_lazyItem5Serializer.Value, other._lazyItem5Serializer.Value) &&
                object.ReferenceEquals(_lazyItem6Serializer.Value, other._lazyItem6Serializer.Value) &&
                object.ReferenceEquals(_lazyItem7Serializer.Value, other._lazyItem7Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public IBsonSerializer GetItemSerializer(int itemNumber)
        {
            return itemNumber switch
            {
                1 => _lazyItem1Serializer.Value,
                2 => _lazyItem2Serializer.Value,
                3 => _lazyItem3Serializer.Value,
                4 => _lazyItem4Serializer.Value,
                5 => _lazyItem5Serializer.Value,
                6 => _lazyItem6Serializer.Value,
                7 => _lazyItem7Serializer.Value,
                _ => throw new IndexOutOfRangeException(nameof(itemNumber))
            };
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ValueTuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            _lazyItem3Serializer.Value.Serialize(context, value.Item3);
            _lazyItem4Serializer.Value.Serialize(context, value.Item4);
            _lazyItem5Serializer.Value.Serialize(context, value.Item5);
            _lazyItem6Serializer.Value.Serialize(context, value.Item6);
            _lazyItem7Serializer.Value.Serialize(context, value.Item7);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7, TRest}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    /// <typeparam name="T5">The type of item 5.</typeparam>
    /// <typeparam name="T6">The type of item 6.</typeparam>
    /// <typeparam name="T7">The type of item 7.</typeparam>
    /// <typeparam name="TRest">The type of the rest item.</typeparam>
    public sealed class ValueTupleSerializer<T1, T2, T3, T4, T5, T6, T7, TRest> : StructSerializerBase<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>, IBsonTupleSerializer
        where TRest : struct
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;
        private readonly Lazy<IBsonSerializer<T4>> _lazyItem4Serializer;
        private readonly Lazy<IBsonSerializer<T5>> _lazyItem5Serializer;
        private readonly Lazy<IBsonSerializer<T6>> _lazyItem6Serializer;
        private readonly Lazy<IBsonSerializer<T7>> _lazyItem7Serializer;
        private readonly Lazy<IBsonSerializer<TRest>> _lazyRestSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6, T7, TRest}"/> class.
        /// </summary>
        public ValueTupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6, T7, TRest}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        /// <param name="item5Serializer">The Item5 serializer.</param>
        /// <param name="item6Serializer">The Item6 serializer.</param>
        /// <param name="item7Serializer">The Item7 serializer.</param>
        /// <param name="restSerializer">The Rest serializer.</param>
        public ValueTupleSerializer(
            IBsonSerializer<T1> item1Serializer,
            IBsonSerializer<T2> item2Serializer,
            IBsonSerializer<T3> item3Serializer,
            IBsonSerializer<T4> item4Serializer,
            IBsonSerializer<T5> item5Serializer,
            IBsonSerializer<T6> item6Serializer,
            IBsonSerializer<T7> item7Serializer,
            IBsonSerializer<TRest> restSerializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }
            if (item2Serializer == null) { throw new ArgumentNullException(nameof(item2Serializer)); }
            if (item3Serializer == null) { throw new ArgumentNullException(nameof(item3Serializer)); }
            if (item4Serializer == null) { throw new ArgumentNullException(nameof(item4Serializer)); }
            if (item5Serializer == null) { throw new ArgumentNullException(nameof(item5Serializer)); }
            if (item6Serializer == null) { throw new ArgumentNullException(nameof(item6Serializer)); }
            if (item7Serializer == null) { throw new ArgumentNullException(nameof(item7Serializer)); }
            if (restSerializer == null) { throw new ArgumentNullException(nameof(restSerializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => item2Serializer);
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => item3Serializer);
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => item4Serializer);
            _lazyItem5Serializer = new Lazy<IBsonSerializer<T5>>(() => item5Serializer);
            _lazyItem6Serializer = new Lazy<IBsonSerializer<T6>>(() => item6Serializer);
            _lazyItem7Serializer = new Lazy<IBsonSerializer<T7>>(() => item7Serializer);
            _lazyRestSerializer = new Lazy<IBsonSerializer<TRest>>(() => restSerializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTupleSerializer{T1, T2, T3, T4, T5, T6, T7, TRest}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public ValueTupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException(nameof(serializerRegistry)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => serializerRegistry.GetSerializer<T1>());
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => serializerRegistry.GetSerializer<T2>());
            _lazyItem3Serializer = new Lazy<IBsonSerializer<T3>>(() => serializerRegistry.GetSerializer<T3>());
            _lazyItem4Serializer = new Lazy<IBsonSerializer<T4>>(() => serializerRegistry.GetSerializer<T4>());
            _lazyItem5Serializer = new Lazy<IBsonSerializer<T5>>(() => serializerRegistry.GetSerializer<T5>());
            _lazyItem6Serializer = new Lazy<IBsonSerializer<T6>>(() => serializerRegistry.GetSerializer<T6>());
            _lazyItem7Serializer = new Lazy<IBsonSerializer<T7>>(() => serializerRegistry.GetSerializer<T7>());
            _lazyRestSerializer = new Lazy<IBsonSerializer<TRest>>(() => serializerRegistry.GetSerializer<TRest>());
        }

        // public properties
        /// <summary>
        /// Gets the Item1 serializer.
        /// </summary>
        public IBsonSerializer<T1> Item1Serializer => _lazyItem1Serializer.Value;

        /// <summary>
        /// Gets the Item2 serializer.
        /// </summary>
        public IBsonSerializer<T2> Item2Serializer => _lazyItem2Serializer.Value;

        /// <summary>
        /// Gets the Item3 serializer.
        /// </summary>
        public IBsonSerializer<T3> Item3Serializer => _lazyItem3Serializer.Value;

        /// <summary>
        /// Gets the Item4 serializer.
        /// </summary>
        public IBsonSerializer<T4> Item4Serializer => _lazyItem4Serializer.Value;

        /// <summary>
        /// Gets the Item5 serializer.
        /// </summary>
        public IBsonSerializer<T5> Item5Serializer => _lazyItem5Serializer.Value;

        /// <summary>
        /// Gets the Item6 serializer.
        /// </summary>
        public IBsonSerializer<T6> Item6Serializer => _lazyItem6Serializer.Value;

        /// <summary>
        /// Gets the Item7 serializer.
        /// </summary>
        public IBsonSerializer<T7> Item7Serializer => _lazyItem7Serializer.Value;

        /// <summary>
        /// Gets the Rest serializer.
        /// </summary>
        public IBsonSerializer<TRest> RestSerializer => _lazyRestSerializer.Value;

        // public methods
        /// <inheritdoc/>
        public override ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            T1 item1 = default;
            T2 item2 = default;
            T3 item3 = default;
            T4 item4 = default;
            T5 item5 = default;
            T6 item6 = default;
            T7 item7 = default;
            TRest rest = default;
            switch (reader.GetCurrentBsonType())
            {
                case BsonType.Array:
                    reader.ReadStartArray();
                    item1 = _lazyItem1Serializer.Value.Deserialize(context);
                    item2 = _lazyItem2Serializer.Value.Deserialize(context);
                    item3 = _lazyItem3Serializer.Value.Deserialize(context);
                    item4 = _lazyItem4Serializer.Value.Deserialize(context);
                    item5 = _lazyItem5Serializer.Value.Deserialize(context);
                    item6 = _lazyItem6Serializer.Value.Deserialize(context);
                    item7 = _lazyItem7Serializer.Value.Deserialize(context);
                    rest = _lazyRestSerializer.Value.Deserialize(context);
                    reader.ReadEndArray();
                    break;

                case BsonType.Document:
                    reader.ReadStartDocument();
                    while (reader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = reader.ReadName();
                        switch (name)
                        {
                            case "Item1": item1 = _lazyItem1Serializer.Value.Deserialize(context); break;
                            case "Item2": item2 = _lazyItem2Serializer.Value.Deserialize(context); break;
                            case "Item3": item3 = _lazyItem3Serializer.Value.Deserialize(context); break;
                            case "Item4": item4 = _lazyItem4Serializer.Value.Deserialize(context); break;
                            case "Item5": item5 = _lazyItem5Serializer.Value.Deserialize(context); break;
                            case "Item6": item6 = _lazyItem6Serializer.Value.Deserialize(context); break;
                            case "Item7": item7 = _lazyItem7Serializer.Value.Deserialize(context); break;
                            case "Rest": rest = _lazyRestSerializer.Value.Deserialize(context); break;
                            default: throw new BsonSerializationException($"Invalid element {name} found while deserializing a ValueTuple.");
                        }
                    }
                    reader.ReadEndDocument();
                    break;

                default:
                    throw new BsonSerializationException($"Cannot deserialize a ValueTuple when BsonType is: {reader.CurrentBsonType}.");
            }

            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueTupleSerializer<T1, T2, T3, T4, T5, T6, T7, TRest> other &&
                object.ReferenceEquals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.ReferenceEquals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.ReferenceEquals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.ReferenceEquals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value) &&
                object.ReferenceEquals(_lazyItem5Serializer.Value, other._lazyItem5Serializer.Value) &&
                object.ReferenceEquals(_lazyItem6Serializer.Value, other._lazyItem6Serializer.Value) &&
                object.ReferenceEquals(_lazyItem7Serializer.Value, other._lazyItem7Serializer.Value) &&
                object.ReferenceEquals(_lazyRestSerializer.Value, other._lazyRestSerializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public IBsonSerializer GetItemSerializer(int itemNumber)
        {
            return itemNumber switch
            {
                1 => _lazyItem1Serializer.Value,
                2 => _lazyItem2Serializer.Value,
                3 => _lazyItem3Serializer.Value,
                4 => _lazyItem4Serializer.Value,
                5 => _lazyItem5Serializer.Value,
                6 => _lazyItem6Serializer.Value,
                7 => _lazyItem7Serializer.Value,
                8 => _lazyRestSerializer.Value,
                _ => throw new IndexOutOfRangeException(nameof(itemNumber))
            };
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            _lazyItem3Serializer.Value.Serialize(context, value.Item3);
            _lazyItem4Serializer.Value.Serialize(context, value.Item4);
            _lazyItem5Serializer.Value.Serialize(context, value.Item5);
            _lazyItem6Serializer.Value.Serialize(context, value.Item6);
            _lazyItem7Serializer.Value.Serialize(context, value.Item7);
            _lazyRestSerializer.Value.Serialize(context, value.Rest);
            context.Writer.WriteEndArray();
        }
    }
}
