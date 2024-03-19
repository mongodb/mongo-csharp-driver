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
using System.Collections;
using System.Collections.Generic;

namespace MongoDB.Driver.TestHelpers
{
    public static class OnlyOnceEnumerable
    {
        public static IEnumerable<T> Create<T>(IEnumerable<T> inner)
            => new OnlyOnceEnumerableImpl<T>(inner);

        private class OnlyOnceEnumerableImpl<T> : IEnumerable<T>
        {
            private bool _wasEnumerated;
            private readonly IEnumerable<T> _innerEnumerable;

            public OnlyOnceEnumerableImpl(IEnumerable<T> innerEnumerable)
            {
                _innerEnumerable = innerEnumerable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (_wasEnumerated)
                {
                    throw new InvalidOperationException("Can only enumerate once.");
                }

                _wasEnumerated = true;
                return _innerEnumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
