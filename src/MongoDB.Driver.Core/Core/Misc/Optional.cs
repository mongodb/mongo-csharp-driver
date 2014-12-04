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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    public static class Optional
    {
        // when the implicit conversion doesn't work calling Create is an alternative
        public static Optional<T> Create<T>(T value)
        {
            return new Optional<T>(value);
        }
    }

    public struct Optional<T>
    {
        private readonly bool _hasValue;
        private readonly T _value;

        public Optional(T value)
        {
            _hasValue = true;
            _value = value;
        }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }

        public bool Replaces(T value)
        {
            return _hasValue && !object.Equals(_value, value);
        }

        public T WithDefault(T value)
        {
            return _hasValue ? _value : value;
        }
    }
}
