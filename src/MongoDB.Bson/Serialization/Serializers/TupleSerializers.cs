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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for a <see cref="Tuple{T1}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    public class TupleSerializer<T1> : SealedClassSerializerBase<Tuple<T1>>
    {
        // private fields
        private readonly IBsonSerializer<T1> _t1Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(
                BsonSerializer.LookupSerializer<T1>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1}"/> class.
        /// </summary>
        /// <param name="t1Serializer">The T1 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> t1Serializer)
        {
            if (t1Serializer == null) { throw new ArgumentNullException("t1Serializer"); }

            _t1Serializer = t1Serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected override Tuple<T1> DeserializeValue(BsonDeserializationContext context)
        {
            context.Reader.ReadStartArray();
            var item1 = context.DeserializeWithChildContext<T1>(_t1Serializer);
            context.Reader.ReadEndArray();

            return new Tuple<T1>(item1);
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, Tuple<T1> value)
        {
            context.Writer.WriteStartArray();
            context.SerializeWithChildContext<T1>(_t1Serializer, value.Item1);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="Tuple{T1, T2}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    public class TupleSerializer<T1, T2> : SealedClassSerializerBase<Tuple<T1, T2>>
    {
        // private fields
        private readonly IBsonSerializer<T1> _t1Serializer;
        private readonly IBsonSerializer<T2> _t2Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(
                BsonSerializer.LookupSerializer<T1>(),
                BsonSerializer.LookupSerializer<T2>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2}"/> class.
        /// </summary>
        /// <param name="t1Serializer">The T1 serializer.</param>
        /// <param name="t2Serializer">The T2 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> t1Serializer,
            IBsonSerializer<T2> t2Serializer)
        {
            if (t1Serializer == null) { throw new ArgumentNullException("t1Serializer"); }
            if (t2Serializer == null) { throw new ArgumentNullException("t2Serializer"); }

            _t1Serializer = t1Serializer;
            _t2Serializer = t2Serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected override Tuple<T1, T2> DeserializeValue(BsonDeserializationContext context)
        {
            context.Reader.ReadStartArray();
            var item1 = context.DeserializeWithChildContext<T1>(_t1Serializer);
            var item2 = context.DeserializeWithChildContext<T2>(_t2Serializer);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2>(item1, item2);
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, Tuple<T1, T2> value)
        {
            context.Writer.WriteStartArray();
            context.SerializeWithChildContext<T1>(_t1Serializer, value.Item1);
            context.SerializeWithChildContext<T2>(_t2Serializer, value.Item2);
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Represents a serializer for a <see cref="Tuple{T1, T2, T3}"/>.
    /// </summary>
    /// <typeparam name="T1">The type of item 1.</typeparam>
    /// <typeparam name="T2">The type of item 2.</typeparam>
    /// <typeparam name="T3">The type of item 3.</typeparam>
    public class TupleSerializer<T1, T2, T3> : SealedClassSerializerBase<Tuple<T1, T2, T3>>
    {
        // private fields
        private readonly IBsonSerializer<T1> _t1Serializer;
        private readonly IBsonSerializer<T2> _t2Serializer;
        private readonly IBsonSerializer<T3> _t3Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(
                BsonSerializer.LookupSerializer<T1>(),
                BsonSerializer.LookupSerializer<T2>(),
                BsonSerializer.LookupSerializer<T3>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3}"/> class.
        /// </summary>
        /// <param name="t1Serializer">The T1 serializer.</param>
        /// <param name="t2Serializer">The T2 serializer.</param>
        /// <param name="t3Serializer">The T3 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> t1Serializer,
            IBsonSerializer<T2> t2Serializer,
            IBsonSerializer<T3> t3Serializer)
        {
            if (t1Serializer == null) { throw new ArgumentNullException("t1Serializer"); }
            if (t2Serializer == null) { throw new ArgumentNullException("t2Serializer"); }
            if (t3Serializer == null) { throw new ArgumentNullException("t3Serializer"); }

            _t1Serializer = t1Serializer;
            _t2Serializer = t2Serializer;
            _t3Serializer = t3Serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected override Tuple<T1, T2, T3> DeserializeValue(BsonDeserializationContext context)
        {
            context.Reader.ReadStartArray();
            var item1 = context.DeserializeWithChildContext<T1>(_t1Serializer);
            var item2 = context.DeserializeWithChildContext<T2>(_t2Serializer);
            var item3 = context.DeserializeWithChildContext<T3>(_t3Serializer);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3>(item1, item2, item3);
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, Tuple<T1, T2, T3> value)
        {
            context.Writer.WriteStartArray();
            context.SerializeWithChildContext<T1>(_t1Serializer, value.Item1);
            context.SerializeWithChildContext<T2>(_t2Serializer, value.Item2);
            context.SerializeWithChildContext<T3>(_t3Serializer, value.Item3);
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
    public class TupleSerializer<T1, T2, T3, T4> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4>>
    {
        // private fields
        private readonly IBsonSerializer<T1> _t1Serializer;
        private readonly IBsonSerializer<T2> _t2Serializer;
        private readonly IBsonSerializer<T3> _t3Serializer;
        private readonly IBsonSerializer<T4> _t4Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(
                BsonSerializer.LookupSerializer<T1>(),
                BsonSerializer.LookupSerializer<T2>(),
                BsonSerializer.LookupSerializer<T3>(),
                BsonSerializer.LookupSerializer<T4>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4}"/> class.
        /// </summary>
        /// <param name="t1Serializer">The T1 serializer.</param>
        /// <param name="t2Serializer">The T2 serializer.</param>
        /// <param name="t3Serializer">The T3 serializer.</param>
        /// <param name="t4Serializer">The T4 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> t1Serializer,
            IBsonSerializer<T2> t2Serializer,
            IBsonSerializer<T3> t3Serializer,
            IBsonSerializer<T4> t4Serializer)
        {
            if (t1Serializer == null) { throw new ArgumentNullException("t1Serializer"); }
            if (t2Serializer == null) { throw new ArgumentNullException("t2Serializer"); }
            if (t3Serializer == null) { throw new ArgumentNullException("t3Serializer"); }
            if (t4Serializer == null) { throw new ArgumentNullException("t4Serializer"); }

            _t1Serializer = t1Serializer;
            _t2Serializer = t2Serializer;
            _t3Serializer = t3Serializer;
            _t4Serializer = t4Serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected override Tuple<T1, T2, T3, T4> DeserializeValue(BsonDeserializationContext context)
        {
            context.Reader.ReadStartArray();
            var item1 = context.DeserializeWithChildContext<T1>(_t1Serializer);
            var item2 = context.DeserializeWithChildContext<T2>(_t2Serializer);
            var item3 = context.DeserializeWithChildContext<T3>(_t3Serializer);
            var item4 = context.DeserializeWithChildContext<T4>(_t4Serializer);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, Tuple<T1, T2, T3, T4> value)
        {
            context.Writer.WriteStartArray();
            context.SerializeWithChildContext<T1>(_t1Serializer, value.Item1);
            context.SerializeWithChildContext<T2>(_t2Serializer, value.Item2);
            context.SerializeWithChildContext<T3>(_t3Serializer, value.Item3);
            context.SerializeWithChildContext<T4>(_t4Serializer, value.Item4);
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
    public class TupleSerializer<T1, T2, T3, T4, T5> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4, T5>>
    {
        // private fields
        private readonly IBsonSerializer<T1> _t1Serializer;
        private readonly IBsonSerializer<T2> _t2Serializer;
        private readonly IBsonSerializer<T3> _t3Serializer;
        private readonly IBsonSerializer<T4> _t4Serializer;
        private readonly IBsonSerializer<T5> _t5Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(
                BsonSerializer.LookupSerializer<T1>(),
                BsonSerializer.LookupSerializer<T2>(),
                BsonSerializer.LookupSerializer<T3>(),
                BsonSerializer.LookupSerializer<T4>(),
                BsonSerializer.LookupSerializer<T5>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5}"/> class.
        /// </summary>
        /// <param name="t1Serializer">The T1 serializer.</param>
        /// <param name="t2Serializer">The T2 serializer.</param>
        /// <param name="t3Serializer">The T3 serializer.</param>
        /// <param name="t4Serializer">The T4 serializer.</param>
        /// <param name="t5Serializer">The T5 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> t1Serializer,
            IBsonSerializer<T2> t2Serializer,
            IBsonSerializer<T3> t3Serializer,
            IBsonSerializer<T4> t4Serializer,
            IBsonSerializer<T5> t5Serializer)
        {
            if (t1Serializer == null) { throw new ArgumentNullException("t1Serializer"); }
            if (t2Serializer == null) { throw new ArgumentNullException("t2Serializer"); }
            if (t3Serializer == null) { throw new ArgumentNullException("t3Serializer"); }
            if (t4Serializer == null) { throw new ArgumentNullException("t4Serializer"); }
            if (t5Serializer == null) { throw new ArgumentNullException("t5Serializer"); }

            _t1Serializer = t1Serializer;
            _t2Serializer = t2Serializer;
            _t3Serializer = t3Serializer;
            _t4Serializer = t4Serializer;
            _t5Serializer = t5Serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected override Tuple<T1, T2, T3, T4, T5> DeserializeValue(BsonDeserializationContext context)
        {
            context.Reader.ReadStartArray();
            var item1 = context.DeserializeWithChildContext<T1>(_t1Serializer);
            var item2 = context.DeserializeWithChildContext<T2>(_t2Serializer);
            var item3 = context.DeserializeWithChildContext<T3>(_t3Serializer);
            var item4 = context.DeserializeWithChildContext<T4>(_t4Serializer);
            var item5 = context.DeserializeWithChildContext<T5>(_t5Serializer);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, Tuple<T1, T2, T3, T4, T5> value)
        {
            context.Writer.WriteStartArray();
            context.SerializeWithChildContext<T1>(_t1Serializer, value.Item1);
            context.SerializeWithChildContext<T2>(_t2Serializer, value.Item2);
            context.SerializeWithChildContext<T3>(_t3Serializer, value.Item3);
            context.SerializeWithChildContext<T4>(_t4Serializer, value.Item4);
            context.SerializeWithChildContext<T5>(_t5Serializer, value.Item5);
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
    public class TupleSerializer<T1, T2, T3, T4, T5, T6> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4, T5, T6>>
    {
        // private fields
        private readonly IBsonSerializer<T1> _t1Serializer;
        private readonly IBsonSerializer<T2> _t2Serializer;
        private readonly IBsonSerializer<T3> _t3Serializer;
        private readonly IBsonSerializer<T4> _t4Serializer;
        private readonly IBsonSerializer<T5> _t5Serializer;
        private readonly IBsonSerializer<T6> _t6Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(
                BsonSerializer.LookupSerializer<T1>(),
                BsonSerializer.LookupSerializer<T2>(),
                BsonSerializer.LookupSerializer<T3>(),
                BsonSerializer.LookupSerializer<T4>(),
                BsonSerializer.LookupSerializer<T5>(),
                BsonSerializer.LookupSerializer<T6>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6}"/> class.
        /// </summary>
        /// <param name="t1Serializer">The T1 serializer.</param>
        /// <param name="t2Serializer">The T2 serializer.</param>
        /// <param name="t3Serializer">The T3 serializer.</param>
        /// <param name="t4Serializer">The T4 serializer.</param>
        /// <param name="t5Serializer">The T5 serializer.</param>
        /// <param name="t6Serializer">The T6 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> t1Serializer,
            IBsonSerializer<T2> t2Serializer,
            IBsonSerializer<T3> t3Serializer,
            IBsonSerializer<T4> t4Serializer,
            IBsonSerializer<T5> t5Serializer,
            IBsonSerializer<T6> t6Serializer)
        {
            if (t1Serializer == null) { throw new ArgumentNullException("t1Serializer"); }
            if (t2Serializer == null) { throw new ArgumentNullException("t2Serializer"); }
            if (t3Serializer == null) { throw new ArgumentNullException("t3Serializer"); }
            if (t4Serializer == null) { throw new ArgumentNullException("t4Serializer"); }
            if (t5Serializer == null) { throw new ArgumentNullException("t5Serializer"); }
            if (t6Serializer == null) { throw new ArgumentNullException("t6Serializer"); }

            _t1Serializer = t1Serializer;
            _t2Serializer = t2Serializer;
            _t3Serializer = t3Serializer;
            _t4Serializer = t4Serializer;
            _t5Serializer = t5Serializer;
            _t6Serializer = t6Serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected override Tuple<T1, T2, T3, T4, T5, T6> DeserializeValue(BsonDeserializationContext context)
        {
            context.Reader.ReadStartArray();
            var item1 = context.DeserializeWithChildContext<T1>(_t1Serializer);
            var item2 = context.DeserializeWithChildContext<T2>(_t2Serializer);
            var item3 = context.DeserializeWithChildContext<T3>(_t3Serializer);
            var item4 = context.DeserializeWithChildContext<T4>(_t4Serializer);
            var item5 = context.DeserializeWithChildContext<T5>(_t5Serializer);
            var item6 = context.DeserializeWithChildContext<T6>(_t6Serializer);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, Tuple<T1, T2, T3, T4, T5, T6> value)
        {
            context.Writer.WriteStartArray();
            context.SerializeWithChildContext<T1>(_t1Serializer, value.Item1);
            context.SerializeWithChildContext<T2>(_t2Serializer, value.Item2);
            context.SerializeWithChildContext<T3>(_t3Serializer, value.Item3);
            context.SerializeWithChildContext<T4>(_t4Serializer, value.Item4);
            context.SerializeWithChildContext<T5>(_t5Serializer, value.Item5);
            context.SerializeWithChildContext<T6>(_t6Serializer, value.Item6);
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
    public class TupleSerializer<T1, T2, T3, T4, T5, T6, T7> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        // private fields
        private readonly IBsonSerializer<T1> _t1Serializer;
        private readonly IBsonSerializer<T2> _t2Serializer;
        private readonly IBsonSerializer<T3> _t3Serializer;
        private readonly IBsonSerializer<T4> _t4Serializer;
        private readonly IBsonSerializer<T5> _t5Serializer;
        private readonly IBsonSerializer<T6> _t6Serializer;
        private readonly IBsonSerializer<T7> _t7Serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(
                BsonSerializer.LookupSerializer<T1>(),
                BsonSerializer.LookupSerializer<T2>(),
                BsonSerializer.LookupSerializer<T3>(),
                BsonSerializer.LookupSerializer<T4>(),
                BsonSerializer.LookupSerializer<T5>(),
                BsonSerializer.LookupSerializer<T6>(),
                BsonSerializer.LookupSerializer<T7>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7}"/> class.
        /// </summary>
        /// <param name="t1Serializer">The T1 serializer.</param>
        /// <param name="t2Serializer">The T2 serializer.</param>
        /// <param name="t3Serializer">The T3 serializer.</param>
        /// <param name="t4Serializer">The T4 serializer.</param>
        /// <param name="t5Serializer">The T5 serializer.</param>
        /// <param name="t6Serializer">The T6 serializer.</param>
        /// <param name="t7Serializer">The T7 serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> t1Serializer,
            IBsonSerializer<T2> t2Serializer,
            IBsonSerializer<T3> t3Serializer,
            IBsonSerializer<T4> t4Serializer,
            IBsonSerializer<T5> t5Serializer,
            IBsonSerializer<T6> t6Serializer,
            IBsonSerializer<T7> t7Serializer)
        {
            if (t1Serializer == null) { throw new ArgumentNullException("t1Serializer"); }
            if (t2Serializer == null) { throw new ArgumentNullException("t2Serializer"); }
            if (t3Serializer == null) { throw new ArgumentNullException("t3Serializer"); }
            if (t4Serializer == null) { throw new ArgumentNullException("t4Serializer"); }
            if (t5Serializer == null) { throw new ArgumentNullException("t5Serializer"); }
            if (t6Serializer == null) { throw new ArgumentNullException("t6Serializer"); }
            if (t7Serializer == null) { throw new ArgumentNullException("t7Serializer"); }

            _t1Serializer = t1Serializer;
            _t2Serializer = t2Serializer;
            _t3Serializer = t3Serializer;
            _t4Serializer = t4Serializer;
            _t5Serializer = t5Serializer;
            _t6Serializer = t6Serializer;
            _t7Serializer = t7Serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected override Tuple<T1, T2, T3, T4, T5, T6, T7> DeserializeValue(BsonDeserializationContext context)
        {
            context.Reader.ReadStartArray();
            var item1 = context.DeserializeWithChildContext<T1>(_t1Serializer);
            var item2 = context.DeserializeWithChildContext<T2>(_t2Serializer);
            var item3 = context.DeserializeWithChildContext<T3>(_t3Serializer);
            var item4 = context.DeserializeWithChildContext<T4>(_t4Serializer);
            var item5 = context.DeserializeWithChildContext<T5>(_t5Serializer);
            var item6 = context.DeserializeWithChildContext<T6>(_t6Serializer);
            var item7 = context.DeserializeWithChildContext<T7>(_t7Serializer);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, Tuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            context.Writer.WriteStartArray();
            context.SerializeWithChildContext<T1>(_t1Serializer, value.Item1);
            context.SerializeWithChildContext<T2>(_t2Serializer, value.Item2);
            context.SerializeWithChildContext<T3>(_t3Serializer, value.Item3);
            context.SerializeWithChildContext<T4>(_t4Serializer, value.Item4);
            context.SerializeWithChildContext<T5>(_t5Serializer, value.Item5);
            context.SerializeWithChildContext<T6>(_t6Serializer, value.Item6);
            context.SerializeWithChildContext<T7>(_t7Serializer, value.Item7);
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
    public class TupleSerializer<T1, T2, T3, T4, T5, T6, T7, TRest> : SealedClassSerializerBase<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
    {
        // private fields
        private readonly IBsonSerializer<T1> _t1Serializer;
        private readonly IBsonSerializer<T2> _t2Serializer;
        private readonly IBsonSerializer<T3> _t3Serializer;
        private readonly IBsonSerializer<T4> _t4Serializer;
        private readonly IBsonSerializer<T5> _t5Serializer;
        private readonly IBsonSerializer<T6> _t6Serializer;
        private readonly IBsonSerializer<T7> _t7Serializer;
        private readonly IBsonSerializer<TRest> _tRestSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7, TRest}"/> class.
        /// </summary>
        public TupleSerializer()
            : this(
                BsonSerializer.LookupSerializer<T1>(),
                BsonSerializer.LookupSerializer<T2>(),
                BsonSerializer.LookupSerializer<T3>(),
                BsonSerializer.LookupSerializer<T4>(),
                BsonSerializer.LookupSerializer<T5>(),
                BsonSerializer.LookupSerializer<T6>(),
                BsonSerializer.LookupSerializer<T7>(),
                BsonSerializer.LookupSerializer<TRest>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleSerializer{T1, T2, T3, T4, T5, T6, T7, TRest}"/> class.
        /// </summary>
        /// <param name="t1Serializer">The T1 serializer.</param>
        /// <param name="t2Serializer">The T2 serializer.</param>
        /// <param name="t3Serializer">The T3 serializer.</param>
        /// <param name="t4Serializer">The T4 serializer.</param>
        /// <param name="t5Serializer">The T5 serializer.</param>
        /// <param name="t6Serializer">The T6 serializer.</param>
        /// <param name="t7Serializer">The T7 serializer.</param>
        /// <param name="tRestSerializer">The TRest serializer.</param>
        public TupleSerializer(
            IBsonSerializer<T1> t1Serializer,
            IBsonSerializer<T2> t2Serializer,
            IBsonSerializer<T3> t3Serializer,
            IBsonSerializer<T4> t4Serializer,
            IBsonSerializer<T5> t5Serializer,
            IBsonSerializer<T6> t6Serializer,
            IBsonSerializer<T7> t7Serializer,
            IBsonSerializer<TRest> tRestSerializer)
        {
            if (t1Serializer == null) { throw new ArgumentNullException("t1Serializer"); }
            if (t2Serializer == null) { throw new ArgumentNullException("t2Serializer"); }
            if (t3Serializer == null) { throw new ArgumentNullException("t3Serializer"); }
            if (t4Serializer == null) { throw new ArgumentNullException("t4Serializer"); }
            if (t5Serializer == null) { throw new ArgumentNullException("t5Serializer"); }
            if (t6Serializer == null) { throw new ArgumentNullException("t6Serializer"); }
            if (t7Serializer == null) { throw new ArgumentNullException("t7Serializer"); }
            if (tRestSerializer == null) { throw new ArgumentNullException("tRestSerializer"); }

            _t1Serializer = t1Serializer;
            _t2Serializer = t2Serializer;
            _t3Serializer = t3Serializer;
            _t4Serializer = t4Serializer;
            _t5Serializer = t5Serializer;
            _t6Serializer = t6Serializer;
            _t7Serializer = t7Serializer;
            _tRestSerializer = tRestSerializer;
        }

        // public methods
        /// <summary>
        /// Deserializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected override Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> DeserializeValue(BsonDeserializationContext context)
        {
            context.Reader.ReadStartArray();
            var item1 = context.DeserializeWithChildContext<T1>(_t1Serializer);
            var item2 = context.DeserializeWithChildContext<T2>(_t2Serializer);
            var item3 = context.DeserializeWithChildContext<T3>(_t3Serializer);
            var item4 = context.DeserializeWithChildContext<T4>(_t4Serializer);
            var item5 = context.DeserializeWithChildContext<T5>(_t5Serializer);
            var item6 = context.DeserializeWithChildContext<T6>(_t6Serializer);
            var item7 = context.DeserializeWithChildContext<T7>(_t7Serializer);
            var rest = context.DeserializeWithChildContext<TRest>(_tRestSerializer);
            context.Reader.ReadEndArray();

            return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="value">The value.</param>
        protected override void SerializeValue(BsonSerializationContext context, Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> value)
        {
            context.Writer.WriteStartArray();
            context.SerializeWithChildContext<T1>(_t1Serializer, value.Item1);
            context.SerializeWithChildContext<T2>(_t2Serializer, value.Item2);
            context.SerializeWithChildContext<T3>(_t3Serializer, value.Item3);
            context.SerializeWithChildContext<T4>(_t4Serializer, value.Item4);
            context.SerializeWithChildContext<T5>(_t5Serializer, value.Item5);
            context.SerializeWithChildContext<T6>(_t6Serializer, value.Item6);
            context.SerializeWithChildContext<T7>(_t7Serializer, value.Item7);
            context.SerializeWithChildContext<TRest>(_tRestSerializer, value.Rest);
            context.Writer.WriteEndArray();
        }
    }
}
