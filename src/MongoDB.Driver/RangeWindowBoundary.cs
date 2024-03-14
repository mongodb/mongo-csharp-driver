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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a boundary for a range window in SetWindowFields.
    /// </summary>
    public abstract class RangeWindowBoundary
    {
        internal RangeWindowBoundary() { } // disallow user defined subclasses
        internal abstract BsonValue Render(IBsonSerializer valueSerializer);
    }

    /// <summary>
    /// Represents a keyword boundary for a range window in SetWindowFields (i.e. "unbounded" or "current").
    /// </summary>
    public sealed class KeywordRangeWindowBoundary : RangeWindowBoundary
    {
        private readonly string _keyword;

        internal KeywordRangeWindowBoundary(string keyword)
        {
            _keyword = Ensure.IsNotNullOrEmpty(keyword, nameof(keyword));
        }

        /// <summary>
        /// The keyword.
        /// </summary>
        public string Keyword => _keyword;

        /// <inheritdoc/>
        public override string ToString() => $"\"{_keyword}\"";

        internal override BsonValue Render(IBsonSerializer valueSerializer) => _keyword;
    }

    /// <summary>
    /// Represents a value boundary for a document window in SetWindowFields.
    /// </summary>
    public abstract class ValueRangeWindowBoundary : RangeWindowBoundary
    {
        internal ValueRangeWindowBoundary() { } // disallow user defined subclasses
        internal abstract Type ValueType { get; }
    }

    /// <summary>
    /// Represents a value boundary for a range window in SetWindowFields.
    /// </summary>
    /// <typeparam name="TValue">The type of the range window boundary.</typeparam>
    public sealed class ValueRangeWindowBoundary<TValue> : ValueRangeWindowBoundary
    {
        private readonly TValue _value;

        /// <summary>
        /// Initializes a new instance of ValueRangeWindowBoundary.
        /// </summary>
        /// <param name="value">The value.</param>
        internal ValueRangeWindowBoundary(TValue value)
        {
            _value = value;
        }

        /// <summary>
        /// The value.
        /// </summary>
        public TValue Value => _value;

        internal override Type ValueType => typeof(TValue);

        /// <inheritdoc/>
        public override string ToString() => _value.ToString();

        internal override BsonValue Render(IBsonSerializer valueSerializer)
        {
            if (valueSerializer == null)
            {
                throw new ArgumentNullException("A value serializer is required to serialize range values.", nameof(valueSerializer));
            }
            return SerializationHelper.SerializeValue(valueSerializer, _value);
        }
    }

    /// <summary>
    /// Represents a time boundary for a range window in SetWindowFields.
    /// </summary>
    public sealed class TimeRangeWindowBoundary : RangeWindowBoundary
    {
        private readonly string _unit;
        private readonly int _value;

        internal TimeRangeWindowBoundary(int value, string unit)
        {
            _value = value;
            _unit = Ensure.IsNotNullOrEmpty(unit, nameof(unit));
        }

        /// <summary>
        /// The unit.
        /// </summary>
        public string Unit => _unit;

        /// <summary>
        /// The value.
        /// </summary>
        public int Value => _value;

        /// <inheritdoc/>
        public override string ToString() => $"{_value} ({_unit})";

        internal override BsonValue Render(IBsonSerializer valueSerializer) => _value;
    }

    internal static class ValueRangeWindowBoundaryConvertingValueSerializerFactory
    {
        private static readonly IReadOnlyDictionary<Type, Type[]> __allowedConversions = new Dictionary<Type, Type[]>
        {
            // sortByType => list of allowed valueTypes
            { typeof(byte), new[] { typeof(sbyte)} },
            { typeof(sbyte), new[] { typeof(byte)} },
            { typeof(short), new[] { typeof(byte), typeof(sbyte), typeof(ushort) } },
            { typeof(ushort), new[] { typeof(byte), typeof(sbyte), typeof(short) } },
            { typeof(int), new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(uint) } },
            { typeof(uint), new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int) } },
            { typeof(long), new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(ulong) } },
            { typeof(ulong), new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long) } },
            { typeof(float), new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(uint) } },
            { typeof(double), new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float) } },
            { typeof(decimal), new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double) } }
        };

        public static IBsonSerializer Create(ValueRangeWindowBoundary boundary, IBsonSerializer sortBySerializer)
        {
            var valueType = boundary.ValueType;
            var sortByType = sortBySerializer.ValueType;

            if (valueType == sortByType)
            {
                return sortBySerializer;
            }

            if (IsAllowedConversion(valueType, sortByType))
            {
                var serializerTypeDescription = typeof(ValueRangeWindowBoundaryConvertingValueSerializer<,>);
                var serializerType = serializerTypeDescription.MakeGenericType(valueType, sortByType);
                var constructorInfo = serializerType.GetConstructors().Single();
                return (IBsonSerializer)constructorInfo.Invoke(new object[] { sortBySerializer });
            }

            throw new InvalidOperationException("SetWindowFields range window value must be of the same type as the sortBy field (or convertible to that type).");
        }

        private static bool IsAllowedConversion(Type valueType, Type sortByType)
        {
            if (__allowedConversions.TryGetValue(sortByType, out var allowedValueTypes))
            {
                return allowedValueTypes.Contains(valueType);
            }

            return false;
        }
    }

    internal class ValueRangeWindowBoundaryConvertingValueSerializer<TValue, TSortBy> : SerializerBase<TValue>
    {
        private readonly IBsonSerializer<TSortBy> _sortBySerializer;

        public ValueRangeWindowBoundaryConvertingValueSerializer(IBsonSerializer<TSortBy> sortBySerializer)
        {
            _sortBySerializer = sortBySerializer;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
        {
            _sortBySerializer.Serialize(context, args, Coerce(value));
        }

        private static TSortBy Coerce(TValue value)
        {
            return (TSortBy)Convert.ChangeType(value, typeof(TSortBy));
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is ValueRangeWindowBoundaryConvertingValueSerializer<TValue, TSortBy> other &&
                object.Equals(_sortBySerializer, other._sortBySerializer);
        }

        public override int GetHashCode() => 0;
    }
}
