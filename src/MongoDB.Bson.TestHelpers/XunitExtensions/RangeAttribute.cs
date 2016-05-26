/* Copyright 2016 MongoDB Inc.
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

namespace MongoDB.Bson.TestHelpers.XunitExtensions
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class RangeAttribute : Attribute, IValueGeneratorAttribute
    {
        private readonly object[] _values;

        public RangeAttribute(int from, int to)
            : this(from, to, 1)
        {
        }

        public RangeAttribute(int from, int to, int step)
        {
            var values = new List<object>();
            for (var value = from; value <= to; value += step)
            {
                values.Add(value);
            }
            _values = values.ToArray();
        }

        public RangeAttribute(double from, double to, double step)
        {
            var values = new List<object>();
            for (var value = from; value <= to; value += step)
            {
                values.Add(value);
            }
            _values = values.ToArray();
        }

        public object[] GenerateValues()
        {
            return _values;
        }
    }
}
