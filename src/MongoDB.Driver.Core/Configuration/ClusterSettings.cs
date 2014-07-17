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
using MongoDB.Driver.Core.Clusters;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents settings for a cluster.
    /// </summary>
    public class ClusterSettings
    {
        // fields
        private readonly ClusterType _clusterType;
        private readonly IReadOnlyList<DnsEndPoint> _endPoints;

        // constructors
        public ClusterSettings()
        {
            _clusterType = ClusterType.Standalone;
            _endPoints = new DnsEndPoint[0];
        }

        internal ClusterSettings(
            ClusterType clusterType,
            IReadOnlyList<DnsEndPoint> endPoints)
        {
            _clusterType = clusterType;
            _endPoints = endPoints;
        }

        //public ClusterSettings(string uriString)
        //    : this(new Uri(Ensure.IsNotNull(uriString, "uriString")))
        //{
        //}

        //public ClusterSettings(Uri uri)
        //{
        //    var parsed = ClusterSettingsUriParser.Parse(uri);
        //    _clusterType = parsed._clusterType;
        //    _endPoints = parsed._endPoints;
        //}

        // properties
        public ClusterType ClusterType
        {
            get { return _clusterType; }
        }

        public IReadOnlyList<DnsEndPoint> EndPoints
        {
            get { return _endPoints; }
        }

        // methods
        public ClusterSettings WithClusterType(ClusterType value)
        {
            return (_clusterType == value) ? this : new Builder(this) { _clusterType = value }.Build();
        }

        public ClusterSettings WithEndPoints(IEnumerable<DnsEndPoint> value)
        {
            var list = value.ToList();
            return _endPoints.SequenceEqual(list) ? this : new Builder(this) { _endPoints = list }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public ClusterType _clusterType;
            public IReadOnlyList<DnsEndPoint> _endPoints;

            // constructors
            public Builder(ClusterSettings other)
            {
                _clusterType = other.ClusterType;
                _endPoints = other.EndPoints;
            }

            // methods
            public ClusterSettings Build()
            {
                return new ClusterSettings(
                    _clusterType,
                    _endPoints);
            }
        }
    }
}
