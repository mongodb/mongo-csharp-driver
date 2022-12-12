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
using System.Diagnostics;
using System.Linq;

namespace MongoDB.TestHelpers.XunitExtensions
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class RandomSeedAttribute : Attribute, IValueGeneratorAttribute
    {
        private readonly object[] _values;

        public RandomSeedAttribute(int[] constantSeeds = null)
        {
            if (constantSeeds == null)
            {
                _values = new object[]
                {
                    int.MaxValue,
                    int.MaxValue / 2,
                    Environment.TickCount ^ Process.GetCurrentProcess().Id
                };
            }
            else
            {
                _values = constantSeeds
                    .Concat(new[] { Environment.TickCount ^ Process.GetCurrentProcess().Id })
                    .Cast<object>()
                    .ToArray();
            }
        }

        public object[] GenerateValues() => _values;
    }
}
