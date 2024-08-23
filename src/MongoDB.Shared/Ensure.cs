/* Copyright 2013-present MongoDB Inc.
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Misc // TODO Change to MongoDB.Shared
{
    [DebuggerStepThrough]
    internal static class Ensure
    {
        public static T? HasValue<T>(T? value, string paramName) where T : struct
        {
            if (!value.HasValue)
            {
                throw new ArgumentException("The Nullable parameter must have a value.", paramName);
            }
            return value;
        }

        public static T IsBetween<T>(T value, T min, T max, string paramName) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                var message = string.Format("Value is not between {1} and {2}: {0}.", value, min, max);
                throw new ArgumentOutOfRangeException(paramName, message);
            }
            return value;
        }

        public static T IsEqualTo<T>(T value, T comparand, string paramName)
        {
            if (!value.Equals(comparand))
            {
                var message = string.Format("Value is not equal to {1}: {0}.", value, comparand);
                throw new ArgumentException(message, paramName);
            }
            return value;
        }

        public static T IsGreaterThan<T>(T value, T comparand, string paramName) where T : IComparable<T>
        {
            if (value.CompareTo(comparand) <= 0)
            {
                var message = $"Value is not greater than {comparand}: {value}.";
                throw new ArgumentOutOfRangeException(paramName, message);
            }
            return value;
        }

        public static T IsGreaterThanOrEqualTo<T>(T value, T comparand, string paramName) where T : IComparable<T>
        {
            if (value.CompareTo(comparand) < 0)
            {
                var message = string.Format("Value is not greater than or equal to {1}: {0}.", value, comparand);
                throw new ArgumentOutOfRangeException(paramName, message);
            }
            return value;
        }

        public static int IsGreaterThanOrEqualToZero(int value, string paramName) =>
            IsGreaterThanOrEqualTo(value, 0, paramName);

        public static long IsGreaterThanOrEqualToZero(long value, string paramName) =>
            IsGreaterThanOrEqualTo(value, 0, paramName);

        public static TimeSpan IsGreaterThanOrEqualToZero(TimeSpan value, string paramName) =>
            IsGreaterThanOrEqualTo(value, TimeSpan.Zero, paramName);

        public static int IsGreaterThanZero(int value, string paramName) =>
            IsGreaterThan(value, 0, paramName);

        public static long IsGreaterThanZero(long value, string paramName) =>
            IsGreaterThan(value, 0, paramName);

        public static double IsGreaterThanZero(double value, string paramName) =>
            IsGreaterThan(value, 0, paramName);

        public static TimeSpan IsGreaterThanZero(TimeSpan value, string paramName) =>
            IsGreaterThan(value, TimeSpan.Zero, paramName);

        public static TimeSpan IsInfiniteOrGreaterThanOrEqualToZero(TimeSpan value, string paramName)
        {
            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                var message = string.Format("Value is not infinite or greater than or equal to zero: {0}.", TimeSpanParser.ToString(value));
                throw new ArgumentOutOfRangeException(paramName, message);
            }
            return value;
        }

        public static TimeSpan IsInfiniteOrGreaterThanZero(TimeSpan value, string paramName)
        {
            if (value == Timeout.InfiniteTimeSpan || value > TimeSpan.Zero)
            {
                return value;
            }
            var message = string.Format("Value is not infinite or greater than zero: {0}.", TimeSpanParser.ToString(value));
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        public static T IsNotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, "Value cannot be null.");
            }
            return value;
        }

        public static IEnumerable<T> IsNotNullAndDoesNotContainAnyNulls<T>(IEnumerable<T> values, string paramName)
            where T : class
        {
            if (values == null)
            {
                throw new ArgumentNullException(paramName, "Values cannot be null.");
            }
            if (values.Any(v => v == null))
            {
                throw new ArgumentNullException(paramName, "Values cannot contain any null items.");
            }
            return values;
        }

        public static string IsNotNullOrEmpty(string value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
            if (value.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }
            return value;
        }

        public static IEnumerable<T> IsNotNullOrEmpty<T>(IEnumerable<T> value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (value is ICollection<T> collection)
            {
                if (collection.Count == 0)
                {
                    throw new ArgumentException("Value cannot be empty.", paramName);
                }
            }
            else if (!value.Any())
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }

            return value;
        }

        public static T IsNull<T>(T value, string paramName) where T : class
        {
            if (value != null)
            {
                throw new ArgumentNullException(paramName, "Value must be null.");
            }
            return value;
        }

        public static T? IsNullOrBetween<T>(T? value, T min, T max, string paramName) where T : struct, IComparable<T>
        {
            if (value != null)
            {
                IsBetween(value.Value, min, max, paramName);
            }
            return value;
        }

        public static int? IsNullOrGreaterThanOrEqualToZero(int? value, string paramName)
        {
            if (value != null)
            {
                IsGreaterThanOrEqualToZero(value.Value, paramName);
            }
            return value;
        }

        public static long? IsNullOrGreaterThanOrEqualToZero(long? value, string paramName)
        {
            if (value != null)
            {
                IsGreaterThanOrEqualToZero(value.Value, paramName);
            }
            return value;
        }

        public static int? IsNullOrGreaterThanZero(int? value, string paramName)
        {
            if (value != null)
            {
                IsGreaterThanZero(value.Value, paramName);
            }
            return value;
        }

        public static long? IsNullOrGreaterThanZero(long? value, string paramName)
        {
            if (value != null)
            {
                IsGreaterThanZero(value.Value, paramName);
            }
            return value;
        }

        public static TimeSpan? IsNullOrGreaterThanZero(TimeSpan? value, string paramName)
        {
            if (value != null)
            {
                IsGreaterThanZero(value.Value, paramName);
            }
            return value;
        }

        public static TimeSpan? IsNullOrInfiniteOrGreaterThanOrEqualToZero(TimeSpan? value, string paramName)
        {
            if (value.HasValue && value.Value < TimeSpan.Zero && value.Value != Timeout.InfiniteTimeSpan)
            {
                var message = string.Format("Value is not null or infinite or greater than or equal to zero: {0}.", TimeSpanParser.ToString(value.Value));
                throw new ArgumentOutOfRangeException(paramName, message);
            }
            return value;
        }

        public static string IsNullOrNotEmpty(string value, string paramName)
        {
            if (value != null && value == "")
            {
                throw new ArgumentException("Value cannot be empty.", paramName);
            }
            return value;
        }

        public static TimeSpan? IsNullOrValidTimeout(TimeSpan? value, string paramName)
        {
            if (value != null)
            {
                IsValidTimeout(value.Value, paramName);
            }
            return value;
        }

        public static TimeSpan IsValidTimeout(TimeSpan value, string paramName)
        {
            if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
            {
                var message = string.Format("Invalid timeout: {0}.", value);
                throw new ArgumentException(message, paramName);
            }
            return value;
        }

        public static void That(bool assertion, string message)
        {
            if (!assertion)
            {
                throw new ArgumentException(message);
            }
        }

        public static void That(bool assertion, string message, string paramName)
        {
            if (!assertion)
            {
                throw new ArgumentException(message, paramName);
            }
        }

        public static T That<T>(T value, Func<T, bool> assertion, string paramName, string message)
        {
            if (!assertion(value))
            {
                throw new ArgumentException(message, paramName);
            }

            return value;
        }
    }
}
