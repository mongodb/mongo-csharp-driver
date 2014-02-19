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

namespace MongoDB.Driver
{
    internal class Range<T> where T : IComparable<T>
    {
        // private fields
        private readonly T _max;
        private readonly T _min;

        // constructors
        public Range(T min, T max)
        {
            _min = min;
            _max = max;
        }

        // public properties
        public T Max
        {
            get { return _max; }
        }

        public T Min
        {
            get { return _min; }
        }

        // public methods
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