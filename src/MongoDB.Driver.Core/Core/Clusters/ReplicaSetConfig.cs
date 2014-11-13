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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents the config of a replica set (as reported by one of the members of the replica set).
    /// </summary>
    public class ReplicaSetConfig
    {
        #region static
        // static properties
        public static ReplicaSetConfig Empty
        {
            get { return new ReplicaSetConfig(Enumerable.Empty<EndPoint>(), null, null, null); }
        }
        #endregion

        // fields
        private readonly List<EndPoint> _members;
        private readonly string _name;
        private readonly EndPoint _primary;
        private readonly int? _version;

        // constructors
        public ReplicaSetConfig(
            IEnumerable<EndPoint> members,
            string name,
            EndPoint primary,
            int? version)
        {
            _members = Ensure.IsNotNull(members, "members").ToList();
            _name = name; // can be null
            _primary = primary; // can be null
            _version = version;
        }

        // properties
        public IReadOnlyList<EndPoint> Members
        {
            get { return _members; }
        }

        public string Name
        {
            get { return _name; }
        }

        public EndPoint Primary
        {
            get { return _primary; }
        }

        public int? Version
        {
            get { return _version; }
        }

        // members
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(ReplicaSetConfig)) { return false; }
            var rhs = (ReplicaSetConfig)obj;
            return
                _members.SequenceEqual(rhs._members) &&
                _name.Equals(rhs._name) &&
                _primary.Equals(rhs._primary) &&
                _version.Equals(rhs._version);
        }

        public override int GetHashCode()
        {
            return new Hasher().HashElements(_members).Hash(_name).Hash(_primary).Hash(_version).GetHashCode();
        }
    }
}
