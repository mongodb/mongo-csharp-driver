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
using System.Text.RegularExpressions;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// An interface implemented by tuple serializers.
    /// </summary>
    public interface IBsonTupleSerializer
    {
        /// <summary>
        /// Gets ths serializer for an item.
        /// </summary>
        /// <param name="itemNumber">The item number.</param>
        /// <returns>The serializer for the item.</returns>
        IBsonSerializer GetItemSerializer(int itemNumber);
    }

    /// <summary>
    /// A factory class for TupleSerializers.
    /// </summary>
    public static class TupleSerializer
    {
        /// <summary>
        /// Creates a TupleSerializer.
        /// </summary>
        /// <param name="itemSerializers">The item serializers.</param>
        /// <returns>A TupleSerializer.</returns>
        public static IBsonSerializer Create(IEnumerable<IBsonSerializer> itemSerializers)
        {
            var itemSerializersArray = itemSerializers.ToArray();
            var tupleSerializerType = CreateTupleSerializerType(itemSerializersArray);
            return (IBsonSerializer)Activator.CreateInstance(tupleSerializerType, itemSerializersArray);

            static Type CreateTupleSerializerType(IBsonSerializer[] itemSerializersArray)
            {
                var itemTypes = itemSerializersArray.Select(s => s.ValueType).ToArray();
                var tupleSerializerTypeDefinition = CreateTupleSerializerTypeDefinition(itemTypes.Length);
                return tupleSerializerTypeDefinition.MakeGenericType(itemTypes);
            }

            static Type CreateTupleSerializerTypeDefinition(int itemCount)
            {
                return itemCount switch
                {
                    1 => typeof(TupleSerializer<>),
                    2 => typeof(TupleSerializer<,>),
                    3 => typeof(TupleSerializer<,,>),
                    4 => typeof(TupleSerializer<,,,>),
                    5 => typeof(TupleSerializer<,,,,>),
                    6 => typeof(TupleSerializer<,,,,,>),
                    7 => typeof(TupleSerializer<,,,,,,>),
                    8 => typeof(TupleSerializer<,,,,,,,>),
                    _ => throw new Exception($"Invalid number of Tuple items : {itemCount}.")
                };
            }
        }

        /// <summary>
        /// Tries to parse an item name to an item number.
        /// </summary>
        /// <param name="itemName">The item name.</param>
        /// <param name="itemNumber">The item number.</param>
        /// <returns>True if the item name was valid.</returns>
        public static bool TryParseItemName(string itemName, out int itemNumber)
        {
            if (itemName == "Rest")
            {
                itemNumber = 8;
                return true;
            }

            var match = Regex.Match(itemName, @"^Item(\d+)$");
            if (match.Success)
            {
                var itemNumberString = match.Groups[1].Value;
                itemNumber = int.Parse(itemNumberString);
                return true;
            }

            itemNumber = default;
            return false;
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="Tuple{T1}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    public sealed class TupleSerializer<T1> : SealedClassSerializerBase<Tuple<T1>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> item1Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public TupleSerializer(IBsonSerializerRegistry serializerRegistry)
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
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is TupleSerializer<T1> other &&
                object.Equals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        // protected methods
        /// <inheritdoc/>
        protected override Tuple<T1> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            var item1 = _lazyItem1Serializer.Value.Deserialize(context);
            context.Reader.ReadEndArray();

            return new Tuple<T1>(item1);
        }

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
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Tuple<T1> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="Tuple{T1, T2}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    public sealed class TupleSerializer<T1, T2> : SealedClassSerializerBase<Tuple<T1, T2>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(BsonSerializer.SerializerRegistry)  //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> item1Serializer,
            IBsonSerializer<T2> item2Serializer)
        {
            if (item1Serializer == null) { throw new ArgumentNullException(nameof(item1Serializer)); }
            if (item2Serializer == null) { throw new ArgumentNullException(nameof(item2Serializer)); }

            _lazyItem1Serializer = new Lazy<IBsonSerializer<T1>>(() => item1Serializer);
            _lazyItem2Serializer = new Lazy<IBsonSerializer<T2>>(() => item2Serializer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public TupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException("serializerRegistry"); }

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
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is TupleSerializer<T1, T1> other &&
                object.Equals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.Equals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        // protected methods
        /// <inheritdoc/>
        protected override Tuple<T1, T2> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            var item1 = _lazyItem1Serializer.Value.Deserialize(context);
            var item2 = _lazyItem2Serializer.Value.Deserialize(context);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2>(item1, item2);
        }

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
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Tuple<T1, T2> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="Tuple{T1, T2, T3}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    public sealed class TupleSerializer<T1, T2, T3> : SealedClassSerializerBase<Tuple<T1, T2, T3>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(BsonSerializer.SerializerRegistry)  //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        public TupleSerializer(
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public TupleSerializer(IBsonSerializerRegistry serializerRegistry)
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
        protected override Tuple<T1, T2, T3> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            var item1 = _lazyItem1Serializer.Value.Deserialize(context);
            var item2 = _lazyItem2Serializer.Value.Deserialize(context);
            var item3 = _lazyItem3Serializer.Value.Deserialize(context);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3>(item1, item2, item3);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is TupleSerializer<T1, T1, T3> other &&
                object.Equals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.Equals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.Equals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value);
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
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Tuple<T1, T2, T3> value)
        {
            context.Writer.WriteStartArray();
            _lazyItem1Serializer.Value.Serialize(context, value.Item1);
            _lazyItem2Serializer.Value.Serialize(context, value.Item2);
            _lazyItem3Serializer.Value.Serialize(context, value.Item3);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="Tuple{T1, T2, T3, T4}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    public sealed class TupleSerializer<T1, T2, T3, T4> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;
        private readonly Lazy<IBsonSerializer<T4>> _lazyItem4Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(BsonSerializer.SerializerRegistry)  //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        public TupleSerializer(
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public TupleSerializer(IBsonSerializerRegistry serializerRegistry)
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
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is TupleSerializer<T1, T1, T3, T4> other &&
                object.Equals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.Equals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.Equals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.Equals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        // protected methods
        /// <inheritdoc/>
        protected override Tuple<T1, T2, T3, T4> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            var item1 = _lazyItem1Serializer.Value.Deserialize(context);
            var item2 = _lazyItem2Serializer.Value.Deserialize(context);
            var item3 = _lazyItem3Serializer.Value.Deserialize(context);
            var item4 = _lazyItem4Serializer.Value.Deserialize(context);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }

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
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Tuple<T1, T2, T3, T4> value)
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
    /// Represents a serializer for a <see cref="Tuple{T1, T2, T3, T4, T5}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    /// <typeparam name="T5">The type of item 5.</typeparam>
    public sealed class TupleSerializer<T1, T2, T3, T4, T5> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4, T5>>, IBsonTupleSerializer
    {
        // private fields
        private readonly Lazy<IBsonSerializer<T1>> _lazyItem1Serializer;
        private readonly Lazy<IBsonSerializer<T2>> _lazyItem2Serializer;
        private readonly Lazy<IBsonSerializer<T3>> _lazyItem3Serializer;
        private readonly Lazy<IBsonSerializer<T4>> _lazyItem4Serializer;
        private readonly Lazy<IBsonSerializer<T5>> _lazyItem5Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(BsonSerializer.SerializerRegistry)  //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        /// <param name="item5Serializer">The Item5 serializer.</param>
        public TupleSerializer(
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public TupleSerializer(IBsonSerializerRegistry serializerRegistry)
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
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is TupleSerializer<T1, T1, T3, T4, T5> other &&
                object.Equals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.Equals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.Equals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.Equals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value) &&
                object.Equals(_lazyItem5Serializer.Value, other._lazyItem5Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        // protected methods
        /// <inheritdoc/>
        protected override Tuple<T1, T2, T3, T4, T5> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            var item1 = _lazyItem1Serializer.Value.Deserialize(context);
            var item2 = _lazyItem2Serializer.Value.Deserialize(context);
            var item3 = _lazyItem3Serializer.Value.Deserialize(context);
            var item4 = _lazyItem4Serializer.Value.Deserialize(context);
            var item5 = _lazyItem5Serializer.Value.Deserialize(context);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }

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
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Tuple<T1, T2, T3, T4, T5> value)
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
    /// Represents a serializer for a <see cref="Tuple{T1, T2, T3, T4, T5, T6}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    /// <typeparam name="T5">The type of item 5.</typeparam>
    /// <typeparam name="T6">The type of item 6.</typeparam>
    public sealed class TupleSerializer<T1, T2, T3, T4, T5, T6> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4, T5, T6>>, IBsonTupleSerializer
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(BsonSerializer.SerializerRegistry)  //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        /// <param name="item5Serializer">The Item5 serializer.</param>
        /// <param name="item6Serializer">The Item6 serializer.</param>
        public TupleSerializer(
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public TupleSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            if (serializerRegistry == null) { throw new ArgumentNullException("serializerRegistry"); }

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
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is TupleSerializer<T1, T1, T3, T4, T5, T6> other &&
                object.Equals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.Equals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.Equals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.Equals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value) &&
                object.Equals(_lazyItem5Serializer.Value, other._lazyItem5Serializer.Value) &&
                object.Equals(_lazyItem6Serializer.Value, other._lazyItem6Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        // protected methods
        /// <inheritdoc/>
        protected override Tuple<T1, T2, T3, T4, T5, T6> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            var item1 = _lazyItem1Serializer.Value.Deserialize(context);
            var item2 = _lazyItem2Serializer.Value.Deserialize(context);
            var item3 = _lazyItem3Serializer.Value.Deserialize(context);
            var item4 = _lazyItem4Serializer.Value.Deserialize(context);
            var item5 = _lazyItem5Serializer.Value.Deserialize(context);
            var item6 = _lazyItem6Serializer.Value.Deserialize(context);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }

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
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Tuple<T1, T2, T3, T4, T5, T6> value)
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
    /// Represents a serializer for a <see cref="Tuple{T1, T2, T3, T4, T5, T6, T7}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    /// <typeparam name="T5">The type of item 5.</typeparam>
    /// <typeparam name="T6">The type of item 6.</typeparam>
    /// <typeparam name="T7">The type of item 7.</typeparam>
    public sealed class TupleSerializer<T1, T2, T3, T4, T5, T6, T7> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4, T5, T6, T7>>, IBsonTupleSerializer
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(BsonSerializer.SerializerRegistry)  //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        /// <param name="item5Serializer">The Item5 serializer.</param>
        /// <param name="item6Serializer">The Item6 serializer.</param>
        /// <param name="item7Serializer">The Item7 serializer.</param>
        public TupleSerializer(
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public TupleSerializer(IBsonSerializerRegistry serializerRegistry)
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
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is TupleSerializer<T1, T1, T3, T4, T5, T6, T7> other &&
                object.Equals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.Equals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.Equals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.Equals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value) &&
                object.Equals(_lazyItem5Serializer.Value, other._lazyItem5Serializer.Value) &&
                object.Equals(_lazyItem6Serializer.Value, other._lazyItem6Serializer.Value) &&
                object.Equals(_lazyItem7Serializer.Value, other._lazyItem7Serializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        // protected methods
        /// <inheritdoc/>
        protected override Tuple<T1, T2, T3, T4, T5, T6, T7> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            var item1 = _lazyItem1Serializer.Value.Deserialize(context);
            var item2 = _lazyItem2Serializer.Value.Deserialize(context);
            var item3 = _lazyItem3Serializer.Value.Deserialize(context);
            var item4 = _lazyItem4Serializer.Value.Deserialize(context);
            var item5 = _lazyItem5Serializer.Value.Deserialize(context);
            var item6 = _lazyItem6Serializer.Value.Deserialize(context);
            var item7 = _lazyItem7Serializer.Value.Deserialize(context);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }

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
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Tuple<T1, T2, T3, T4, T5, T6, T7> value)
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
    /// Represents a serializer for a <see cref="Tuple{T1, T2, T3, T4, T5, T6, T7, TRest}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    /// <typeparam name="T4">The type of item 4.</typeparam>
    /// <typeparam name="T5">The type of item 5.</typeparam>
    /// <typeparam name="T6">The type of item 6.</typeparam>
    /// <typeparam name="T7">The type of item 7.</typeparam>
    /// <typeparam name="TRest">The type of the rest item.</typeparam>
    public sealed class TupleSerializer<T1, T2, T3, T4, T5, T6, T7, TRest> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>>, IBsonTupleSerializer
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7, TRest}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(BsonSerializer.SerializerRegistry)  //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7, TRest}"/> class.
        /// </summary>
        /// <param name="item1Serializer">The Item1 serializer.</param>
        /// <param name="item2Serializer">The Item2 serializer.</param>
        /// <param name="item3Serializer">The Item3 serializer.</param>
        /// <param name="item4Serializer">The Item4 serializer.</param>
        /// <param name="item5Serializer">The Item5 serializer.</param>
        /// <param name="item6Serializer">The Item6 serializer.</param>
        /// <param name="item7Serializer">The Item7 serializer.</param>
        /// <param name="restSerializer">The Rest serializer.</param>
        public TupleSerializer(
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
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7, TRest}" /> class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public TupleSerializer(IBsonSerializerRegistry serializerRegistry)
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
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is TupleSerializer<T1, T1, T3, T4, T5, T6, T7, TRest> other &&
                object.Equals(_lazyItem1Serializer.Value, other._lazyItem1Serializer.Value) &&
                object.Equals(_lazyItem2Serializer.Value, other._lazyItem2Serializer.Value) &&
                object.Equals(_lazyItem3Serializer.Value, other._lazyItem3Serializer.Value) &&
                object.Equals(_lazyItem4Serializer.Value, other._lazyItem4Serializer.Value) &&
                object.Equals(_lazyItem5Serializer.Value, other._lazyItem5Serializer.Value) &&
                object.Equals(_lazyItem6Serializer.Value, other._lazyItem6Serializer.Value) &&
                object.Equals(_lazyItem7Serializer.Value, other._lazyItem7Serializer.Value) &&
                object.Equals(_lazyRestSerializer.Value, other._lazyRestSerializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        // protected methods
        /// <inheritdoc/>
        protected override Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            context.Reader.ReadStartArray();
            var item1 = _lazyItem1Serializer.Value.Deserialize(context);
            var item2 = _lazyItem2Serializer.Value.Deserialize(context);
            var item3 = _lazyItem3Serializer.Value.Deserialize(context);
            var item4 = _lazyItem4Serializer.Value.Deserialize(context);
            var item5 = _lazyItem5Serializer.Value.Deserialize(context);
            var item6 = _lazyItem6Serializer.Value.Deserialize(context);
            var item7 = _lazyItem7Serializer.Value.Deserialize(context);
            var rest = _lazyRestSerializer.Value.Deserialize(context);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
        }

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
        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> value)
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
