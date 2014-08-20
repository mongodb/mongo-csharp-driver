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
using System.Net;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Servers
{
    public sealed class ServerId : IEquatable<ServerId>
    {
        // fields
        private readonly ClusterId _clusterId;
        private readonly EndPoint _endPoint;

        // constructors
        public ServerId(ClusterId clusterId, EndPoint endPoint)
        {
            _clusterId = Ensure.IsNotNull(clusterId, "clusterId");
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
        }

        // properties
        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }

        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        // methods
        public bool Equals(ServerId other)
        {
            if (other == null)
            {
                return false;
            }
            return
                _clusterId.Equals(other._clusterId) &&
                _endPoint.Equals(other._endPoint);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ServerId);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_clusterId)
                .Hash(_endPoint)
                .GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{{ ClusterId : {0}, EndPoint : \"{1}\" }}", _clusterId, _endPoint);
        }
    }
}
