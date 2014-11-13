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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a replica set member tag set.
    /// </summary>
    public sealed class TagSet : IEquatable<TagSet>
    {
        // fields
        private readonly IReadOnlyList<Tag> _tags;

        // constructor
        public TagSet()
        {
            _tags = new Tag[0];
        }

        public TagSet(IEnumerable<Tag> tags)
        {
            _tags = Ensure.IsNotNull(tags, "tags").ToList();
        }

        // properties
        public bool IsEmpty
        {
            get { return _tags.Count == 0; }
        }

        public IReadOnlyList<Tag> Tags
        {
            get { return _tags; }
        }

        // methods
        public bool ContainsAll(TagSet required)
        {
            return required.Tags.All(t => _tags.Contains(t));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TagSet);
        }

        public bool Equals(TagSet rhs)
        {
            if (rhs == null)
            {
                return false;
            }
            return _tags.SequenceEqual(rhs._tags);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .HashElements(_tags)
                .GetHashCode();
        }

        public override string ToString()
        {
            if (_tags.Count == 0)
            {
                return "{ }";
            }
            else
            {
                return string.Format("{{ {0} }}", string.Join(", ", _tags.Select(t => t.ToString()).ToArray()));
            }
        }
    }
}
