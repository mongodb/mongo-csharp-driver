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
    /// Represents a range for the $densify stage.
    /// </summary>
    public abstract class DensifyRange
    {
        #region static
        /// <summary>
        /// Creates a DensifyRange with DateTime bounds.
        /// </summary>
        /// <param name="lowerBound">The lower bound.</param>
        /// <param name="upperBound">The upper bound.</param>
        /// <param name="step">The step.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>A DensifyRange with DateTime bounds.</returns>
        public static DensifyRange DateTime(DateTime lowerBound, DateTime upperBound, int step, DensifyDateTimeUnit unit)

        {
            return new DensifyDateTimeRange(new DensifyLowerUpperDateTimeBounds(lowerBound, upperBound), step, unit);
        }

        /// <summary>
        /// Creates a DensifyRange with DateTime bounds.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="step">The step.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>A DensifyRange with DateTime bounds.</returns>
        public static DensifyRange DateTime(DensifyBounds bounds, int step, DensifyDateTimeUnit unit)
        {
            return new DensifyDateTimeRange(bounds.ToDateTimeBounds(), step, unit);
        }

        /// <summary>
        /// Creates a DensifyRange with numeric bounds.
        /// </summary>
        /// <typeparam name="TNumber">The numeric type.</typeparam>
        /// <param name="lowerBound">The lower bound.</param>
        /// <param name="upperBound">The upper bound.</param>
        /// <param name="step">The step.</param>
        /// <returns>A DensifyRange with numeric bounds.</returns>
        public static DensifyRange Numeric<TNumber>(TNumber lowerBound, TNumber upperBound, TNumber step)
        {
            return new DensifyNumericRange<TNumber>(new DensifyLowerUpperNumericBounds<TNumber>(lowerBound, upperBound), step);
        }

        /// <summary>
        /// Creates a DensifyRange with numeric bounds.
        /// </summary>
        /// <typeparam name="TNumber">The numeric type.</typeparam>
        /// <param name="bounds">The bounds.</param>
        /// <param name="step">The step.</param>
        /// <returns>A DensifyRange with numeric bounds.</returns>
        public static DensifyRange Numeric<TNumber>(DensifyBounds bounds, TNumber step)
        {
            return new DensifyNumericRange<TNumber>(bounds.ToNumericBounds<TNumber>(), step);
        }
        #endregion

        /// <summary>
        /// Renders the range as a BsonDocument.
        /// </summary>
        /// <returns>The rendered range.</returns>
        public abstract BsonDocument Render();

        /// <inheritdoc/>
        public override string ToString() => Render().ToJson();
    }

    /// <summary>
    /// Represents keyword densify bounds.
    /// </summary>
    public sealed class DensifyBounds
    {
        #region static
        private static readonly DensifyBounds __full = new DensifyBounds("full");
        private static readonly DensifyBounds __partition = new DensifyBounds("partition");

        /// <summary>
        /// Gets a DensifyBounds representing the "full" bounds.
        /// </summary>
        public static DensifyBounds Full => __full;

        /// <summary>
        /// Gets a DensifyBounds representing the "partition" bounds.
        /// </summary>
        public static DensifyBounds Partition => __partition;
        #endregion

        private readonly string _keyword;

        private DensifyBounds(string keyword)
        {
            _keyword = Ensure.IsNotNull(keyword, nameof(keyword));
        }

        /// <summary>
        /// Gets the keyword.
        /// </summary>
        public string Keyword => _keyword;

        internal DensifyKeywordDateTimeBounds ToDateTimeBounds()
        {
            return _keyword switch
            {
                "full" => DensifyKeywordDateTimeBounds.Full,
                "partition" => DensifyKeywordDateTimeBounds.Partition,
                _ => throw new ArgumentException($"Invalid DensifyBounds keyword: {_keyword}.", nameof(_keyword))
            };
        }

        internal DensifyKeywordNumericBounds<TNumber> ToNumericBounds<TNumber>()
        {
            return _keyword switch
            {
                "full" => DensifyKeywordNumericBounds<TNumber>.Full,
                "partition" => DensifyKeywordNumericBounds<TNumber>.Partition,
                _ => throw new ArgumentException($"Invalid DensifyBounds keyword: {_keyword}.", nameof(_keyword))
            };
        }
    }

    /// <summary>
    /// Represents a numeric densify range.
    /// </summary>
    /// <typeparam name="TNumber">The numeric type.</typeparam>
    public sealed class DensifyNumericRange<TNumber> : DensifyRange
    {
        #region static
        internal static void EnsureIsValidNumericType()
        {
            switch (Type.GetTypeCode(typeof(TNumber)))
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
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
                short shortNumber => shortNumber,
                _ => throw new InvalidOperationException($"Unexpected numeric type: {number.GetType().FullName}.")
            };
        }
        #endregion

        private readonly DensifyNumericBounds<TNumber> _bounds;
        private readonly TNumber _step;

        /// <summary>
        /// Initializes a new instance of DensifyNumericRange.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="step">The step.</param>
        public DensifyNumericRange(DensifyNumericBounds<TNumber> bounds, TNumber step)
        {
            EnsureIsValidNumericType();
            _bounds = Ensure.IsNotNull(bounds, nameof(bounds));
            _step = step;
        }

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        public DensifyNumericBounds<TNumber> Bounds => _bounds;

        /// <summary>
        /// Gets the step.
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
    /// Represents a numeric densify bounds.
    /// </summary>
    /// <typeparam name="TNumber">The numeric type.</typeparam>
    public abstract class DensifyNumericBounds<TNumber>
    {
        /// <summary>
        /// Renders the bounds as a BsonValue.
        /// </summary>
        /// <returns>The rendered bounds.</returns>
        public abstract BsonValue Render();
    }

    /// <summary>
    /// Represents a keyword numeric densify bounds.
    /// </summary>
    /// <typeparam name="TNumber">The numeric type.</typeparam>
    public sealed class DensifyKeywordNumericBounds<TNumber> : DensifyNumericBounds<TNumber>
    {
        #region static
        private readonly static DensifyKeywordNumericBounds<TNumber> __full = new DensifyKeywordNumericBounds<TNumber>("full");
        private readonly static DensifyKeywordNumericBounds<TNumber> __partition = new DensifyKeywordNumericBounds<TNumber>("partition");

        internal static DensifyKeywordNumericBounds<TNumber> Full => __full;
        internal static DensifyKeywordNumericBounds<TNumber> Partition => __partition;
        #endregion

        private readonly string _keyword;

        internal DensifyKeywordNumericBounds(string keyword)
        {
            _keyword = Ensure.IsNotNullOrEmpty(keyword, nameof(keyword));
        }

        /// <summary>
        /// Gets the keyword.
        /// </summary>
        public string Keyword => _keyword;

        /// <inheritdoc/>
        public override BsonValue Render() => _keyword;
    }

    /// <summary>
    /// Represents a numeric densify bounds with lower and upper bounds.
    /// </summary>
    /// <typeparam name="TNumber">The numeric type.</typeparam>
    public sealed class DensifyLowerUpperNumericBounds<TNumber> : DensifyNumericBounds<TNumber>
    {
        private readonly TNumber _lowerBound;
        private readonly TNumber _upperBound;

        /// <summary>
        /// Initializes an instance of DensifyLowerUpperNumericBounds.
        /// </summary>
        /// <param name="lowerBound">The lower bound.</param>
        /// <param name="upperBound">The upper bound.</param>
        public DensifyLowerUpperNumericBounds(TNumber lowerBound, TNumber upperBound)
        {
            _lowerBound = lowerBound;
            _upperBound = upperBound;
        }

        /// <summary>
        /// Gets the lower bound.
        /// </summary>
        public TNumber LowerBound => _lowerBound;

        /// <summary>
        /// Gets the upper bound.
        /// </summary>
        public TNumber UpperBound => _upperBound;

        /// <inheritdoc/>
        public override BsonValue Render() => new BsonArray { RenderBound(_lowerBound), RenderBound(_upperBound) };

        private BsonValue RenderBound(TNumber bound) => DensifyNumericRange<TNumber>.RenderNumber(bound);
    }

    /// <summary>
    /// Represents a DateTime densify range.
    /// </summary>
    public sealed class DensifyDateTimeRange : DensifyRange
    {
        private readonly DensifyDateTimeBounds _bounds;
        private readonly long _step;
        private readonly DensifyDateTimeUnit _unit;

        /// <summary>
        /// Initializes an instance of DensifyDateTimeRange.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="step">The step.</param>
        /// <param name="unit">The unit.</param>
        public DensifyDateTimeRange(DensifyDateTimeBounds bounds, int step, DensifyDateTimeUnit unit)
        {
            _bounds = Ensure.IsNotNull(bounds, nameof(bounds));
            _step = Ensure.IsGreaterThanZero(step, nameof(step));
            _unit = unit;
        }

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        public DensifyDateTimeBounds Bounds => _bounds;

        /// <summary>
        /// Gets the step.
        /// </summary>
        public long Step => _step;

        /// <summary>
        /// Gets the unit.
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
    /// Represents a DateTime densify bounds.
    /// </summary>
    public abstract class DensifyDateTimeBounds
    {
        /// <summary>
        /// Renders the bounds as a BsonValue.
        /// </summary>
        /// <returns>The rendered bounds.</returns>
        public abstract BsonValue Render();
    }

    /// <summary>
    /// Represents a keyword DateTime densify bounds.
    /// </summary>
    public sealed class DensifyKeywordDateTimeBounds : DensifyDateTimeBounds
    {
        #region static
        private readonly static DensifyKeywordDateTimeBounds __full = new DensifyKeywordDateTimeBounds("full");
        private readonly static DensifyKeywordDateTimeBounds __partition = new DensifyKeywordDateTimeBounds("partition");

        internal static DensifyKeywordDateTimeBounds Full => __full;
        internal static DensifyKeywordDateTimeBounds Partition => __partition;
        #endregion

        private readonly string _keyword;

        /// <summary>
        /// Initializes an instance of DensifyKeywordDateTimeBounds.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        internal DensifyKeywordDateTimeBounds(string keyword)
        {
            _keyword = Ensure.IsNotNullOrEmpty(keyword, nameof(keyword));
        }

        /// <summary>
        /// Gets the keyword.
        /// </summary>
        public string Keyword => _keyword;

        /// <inheritdoc/>
        public override BsonValue Render() => _keyword;
    }

    /// <summary>
    /// Represents a DateTime densify bounds with lower and upper bounds.
    /// </summary>
    public sealed class DensifyLowerUpperDateTimeBounds : DensifyDateTimeBounds
    {
        private readonly DateTime _lowerBound;
        private readonly DateTime _upperBound;

        /// <summary>
        /// Initializes an instance of DensifyLowerUpperDateTimeBounds.
        /// </summary>
        /// <param name="lowerBound">The lower bound.</param>
        /// <param name="upperBound">The upper bound.</param>
        public DensifyLowerUpperDateTimeBounds(DateTime lowerBound, DateTime upperBound)
        {
            _lowerBound = lowerBound;
            _upperBound = upperBound;
        }

        /// <summary>
        /// Gets the lower bound.
        /// </summary>
        public DateTime LowerBound => _lowerBound;

        /// <summary>
        /// Gets the upper bound.
        /// </summary>
        public DateTime UpperBound => _upperBound;

        /// <inheritdoc/>
        public override BsonValue Render() => new BsonArray { _lowerBound, _upperBound };
    }

    /// <summary>
    /// Represents a densify DateTime unit.
    /// </summary>
    public enum DensifyDateTimeUnit
    {
        /// <summary>
        /// Milliseconds.
        /// </summary>
        Milliseconds = 1,

        /// <summary>
        /// Seconds.
        /// </summary>
        Seconds,

        /// <summary>
        /// Minutes.
        /// </summary>
        Minutes,

        /// <summary>
        /// Hours.
        /// </summary>
        Hours,

        /// <summary>
        /// Days.
        /// </summary>
        Days,

        /// <summary>
        /// Weeks.
        /// </summary>
        Weeks,

        /// <summary>
        /// Months.
        /// </summary>
        Months,

        /// <summary>
        /// Quarters.
        /// </summary>
        Quarters,

        /// <summary>
        /// Years.
        /// </summary>
        Years
    }
}
