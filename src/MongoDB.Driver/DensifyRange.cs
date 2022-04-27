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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class DensifyRange
    {
        #region static
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lowerBound"></param>
        /// <param name="upperBound"></param>
        /// <param name="step"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static DensifyRange DateTime(DateTime lowerBound, DateTime upperBound, int step, DensifyDateTimeUnit unit)
        {
            return new DensifyDateTimeRange(new DensifyValuesDateTimeBounds(lowerBound, upperBound), step, unit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="step"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static DensifyRange DateTime(DensifyBounds bounds, int step, DensifyDateTimeUnit unit)
        {
            return new DensifyDateTimeRange(new DensifyKeywordDateTimeBounds(bounds.Keyword), step, unit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNumber"></typeparam>
        /// <param name="lowerBound"></param>
        /// <param name="upperBound"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static DensifyRange Numeric<TNumber>(TNumber lowerBound, TNumber upperBound, TNumber step)
        {
            return new DensifyNumericRange<TNumber>(new DensifyValuesNumericBounds<TNumber>(lowerBound, upperBound), step);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNumber"></typeparam>
        /// <param name="bounds"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static DensifyRange Numeric<TNumber>(DensifyBounds bounds, TNumber step)
        {
            return new DensifyNumericRange<TNumber>(new DensifyKeywordNumericBounds<TNumber>(bounds.Keyword), step);
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract BsonDocument Render();
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class DensifyBounds
    {
        #region static
        private static readonly DensifyBounds __full = new DensifyBounds("full");
        private static readonly DensifyBounds __partition = new DensifyBounds("partition");

        /// <summary>
        /// 
        /// </summary>
        public static DensifyBounds Full => __full;

        /// <summary>
        /// 
        /// </summary>
        public static DensifyBounds Partition => __partition;
        #endregion

        private readonly string _keyword;

        private DensifyBounds(string keyword)
        {
            _keyword = Ensure.IsNotNull(keyword, nameof(keyword));
        }

        /// <summary>
        /// 
        /// </summary>
        public string Keyword => _keyword;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TNumber"></typeparam>
    public sealed class DensifyNumericRange<TNumber> : DensifyRange
    {
        #region static
        internal static void EnsureTNumberIsValidNumericType()
        {
            switch (Type.GetTypeCode(typeof(TNumber)))
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Single:
                    break;

                default:
                    throw new ArgumentException($"TNumber is not a valid numeric type for DensifyNumericRange: {typeof(TNumber).FullName}.");
            }
        }

        internal static BsonValue RenderNumber(TNumber number)
        {
            return number switch
            {
                decimal decimalNumber => decimalNumber,
                double doubleNumber => doubleNumber,
                float floatNumber => floatNumber,
                int intNumber => intNumber,
                long longNumber => longNumber,
                _ => throw new InvalidOperationException($"Unexpected numeric type: {number.GetType().FullName}.")
            };
        }
        #endregion

        private readonly DensifyNumericBounds<TNumber> _bounds;
        private readonly TNumber _step;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="step"></param>
        public DensifyNumericRange(DensifyNumericBounds<TNumber> bounds, TNumber step)
        {
            EnsureTNumberIsValidNumericType();
            _bounds = Ensure.IsNotNull(bounds, nameof(bounds));
            _step = step;
        }

        /// <summary>
        /// 
        /// </summary>
        public DensifyNumericBounds<TNumber> Bounds => _bounds;

        /// <summary>
        /// 
        /// </summary>
        public TNumber Step => _step;

        /// <inheritdoc/>
        public override BsonDocument Render()
        {
            return new BsonDocument
            {
                { "step", RenderNumber(_step) },
                { "bounds", _bounds.Render() }
            };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TNumber"></typeparam>
    public abstract class DensifyNumericBounds<TNumber>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract BsonValue Render();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TNumber"></typeparam>
    public sealed class DensifyKeywordNumericBounds<TNumber> : DensifyNumericBounds<TNumber>
    {
        private readonly string _keyword;

        internal DensifyKeywordNumericBounds(string keyword)
        {
            _keyword = Ensure.IsNotNullOrEmpty(keyword, nameof(keyword));
        }

        /// <summary>
        /// 
        /// </summary>
        public string Keyword => _keyword;

        /// <inheritdoc/>
        public override BsonValue Render() => _keyword;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TNumber"></typeparam>
    public sealed class DensifyValuesNumericBounds<TNumber> : DensifyNumericBounds<TNumber>
    {
        private readonly TNumber _lowerBound;
        private readonly TNumber _upperBound;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lowerBound"></param>
        /// <param name="upperBound"></param>
        public DensifyValuesNumericBounds(TNumber lowerBound, TNumber upperBound)
        {
            _lowerBound = lowerBound;
            _upperBound = upperBound;
        }

        /// <summary>
        /// 
        /// </summary>
        public TNumber LowerBound => _lowerBound;

        /// <summary>
        /// 
        /// </summary>
        public TNumber UpperBound => _upperBound;

        /// <inheritdoc/>
        public override BsonValue Render() => new BsonArray { RenderBound(_lowerBound), RenderBound(_upperBound) };

        private BsonValue RenderBound(TNumber bound) => DensifyNumericRange<TNumber>.RenderNumber(bound);
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class DensifyDateTimeRange : DensifyRange
    {
        private readonly DensifyDateTimeBounds _bounds;
        private readonly long _step;
        private readonly DensifyDateTimeUnit _unit;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="step"></param>
        /// <param name="unit"></param>
        public DensifyDateTimeRange(DensifyDateTimeBounds bounds, int step, DensifyDateTimeUnit unit)
        {
            _bounds = Ensure.IsNotNull(bounds, nameof(bounds));
            _step = Ensure.IsGreaterThanZero(step, nameof(step));
            _unit = unit;
        }

        /// <summary>
        /// 
        /// </summary>
        public DensifyDateTimeBounds Bounds => _bounds;

        /// <summary>
        /// 
        /// </summary>
        public long Step => _step;

        /// <summary>
        /// 
        /// </summary>
        public DensifyDateTimeUnit Unit => _unit;

        /// <inheritdoc/>
        public override BsonDocument Render()
        {
            return new BsonDocument
            {
                { "step", _step },
                { "unit", RenderUnit(_unit) },
                { "bounds", _bounds.Render() }
            };
        }

        private BsonValue RenderUnit(DensifyDateTimeUnit unit)
        {
            return unit switch
            {
                DensifyDateTimeUnit.Milliseconds => "millisecond",
                DensifyDateTimeUnit.Seconds => "second",
                DensifyDateTimeUnit.Minutes => "minute",
                DensifyDateTimeUnit.Hours => "hour",
                DensifyDateTimeUnit.Days => "day",
                DensifyDateTimeUnit.Weeks => "week",
                DensifyDateTimeUnit.Months => "month",
                DensifyDateTimeUnit.Quarters => "quarter",
                DensifyDateTimeUnit.Years => "year",
                _ => throw new ArgumentException($"Unexpected DensifyDateTimeUnit: {unit}.", nameof(unit))
            };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class DensifyDateTimeBounds
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract BsonValue Render();
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class DensifyKeywordDateTimeBounds : DensifyDateTimeBounds
    {
        private readonly string _keyword;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyword"></param>
        internal DensifyKeywordDateTimeBounds(string keyword)
        {
            _keyword = Ensure.IsNotNullOrEmpty(keyword, nameof(keyword));
        }

        /// <summary>
        /// 
        /// </summary>
        public string Keyword => _keyword;

        /// <inheritdoc/>
        public override BsonValue Render() => _keyword;
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class DensifyValuesDateTimeBounds : DensifyDateTimeBounds
    {
        private readonly DateTime _lowerBound;
        private readonly DateTime _upperBound;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lowerBound"></param>
        /// <param name="upperBound"></param>
        public DensifyValuesDateTimeBounds(DateTime lowerBound, DateTime upperBound)
        {
            _lowerBound = lowerBound;
            _upperBound = upperBound;
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime LowerBound => _lowerBound;

        /// <summary>
        /// 
        /// </summary>
        public DateTime UpperBound => _upperBound;

        /// <inheritdoc/>
        public override BsonValue Render() => new BsonArray { _lowerBound, _upperBound };
    }

    /// <summary>
    /// 
    /// </summary>
    public enum DensifyDateTimeUnit
    {
        /// <summary>
        /// 
        /// </summary>
        Milliseconds = 1,

        /// <summary>
        /// 
        /// </summary>
        Seconds,

        /// <summary>
        /// 
        /// </summary>
        Minutes,

        /// <summary>
        /// 
        /// </summary>
        Hours,

        /// <summary>
        /// 
        /// </summary>
        Days,

        /// <summary>
        /// 
        /// </summary>
        Weeks,

        /// <summary>
        /// 
        /// </summary>
        Months,

        /// <summary>
        /// 
        /// </summary>
        Quarters,

        /// <summary>
        /// 
        /// </summary>
        Years
    }
}
