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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a replica set member tag.
    /// </summary>
    public class Tag : IEquatable<Tag>
    {
        // fields
        private readonly string _name;
        private readonly string _value;

        // constructors
        public Tag(string name, string value)
        {
            _name = Ensure.IsNotNull(name, "name");
            _value = Ensure.IsNotNull(value, "value");
        }

        // properties
        public string Name
        {
            get { return _name; }
        }

        public string Value
        {
            get { return _value; }
        }

        // methods
        public override bool Equals(object obj)
        {
            return Equals(obj as Tag);
        }

        public bool Equals(Tag rhs)
        {
            if (object.ReferenceEquals(rhs, null) || rhs.GetType() != typeof(Tag))
            {
                return false;
            }
            return _name == rhs._name && _value == rhs._value;
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_name)
                .Hash(_value)
                .GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0} : {1}", _name, _value);
        }
    }
}
