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
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Misc
{
    public sealed class Range<T> : IEquatable<Range<T>> where T : IComparable<T>
    {
        // fields
        private readonly T _max;
        private readonly T _min;

        // constructors
        public Range(T min, T max)
        {
            if (min.CompareTo(max) > 0)
            {
                throw new ArgumentOutOfRangeException("min", "Must be less than max.");
            }

            _min = min;
            _max = max;
        }

        // properties
        public T Max
        {
            get { return _max; }
        }

        public T Min
        {
            get { return _min; }
        }

        // methods
        public bool Equals(Range<T> other)
        {
            if (other == null)
            {
                return false;
            }

            return _max.CompareTo(other._max) == 0 &&
                _min.CompareTo(other._min) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Range<T>);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_max)
                .Hash(_min)
                .GetHashCode();
        }

        public bool Overlaps(Range<T> other)
        {
            return _min.CompareTo(other.Max) <= 0 && _max.CompareTo(other.Min) >= 0;
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", _min, _max);
        }
    }
}