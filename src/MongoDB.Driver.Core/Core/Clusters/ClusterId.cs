/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Clusters
{
    [Serializable]
    public sealed class ClusterId : IEquatable<ClusterId>
    {
        // fields
        private readonly int _value;

        // constructors
        public ClusterId()
            : this(IdGenerator<ClusterId>.GetNextId())
        {
        }

        public ClusterId(int value)
        {
            _value = value;
        }

        // properties
        public int Value
        {
            get { return _value; }
        }

        // methods
        public bool Equals(ClusterId other)
        {
            if (other == null)
            {
                return false;
            }
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ClusterId);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
