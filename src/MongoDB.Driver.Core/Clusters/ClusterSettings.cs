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
using System.Net;
using MongoDB.Driver.Core.Clusters.Events;
using MongoDB.Driver.Core.Connections.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents settings for a cluster.
    /// </summary>
    public class ClusterSettings
    {
        // fields
        private readonly IClusterListener _clusterListener;
        private readonly ClusterType _clusterType;
        private readonly IReadOnlyList<DnsEndPoint> _endPoints;
        private readonly IMessageListener _messageListener;
        private readonly ServerSettings _serverSettings;

        // constructors
        public ClusterSettings()
        {
            _clusterType = ClusterType.Standalone;
            _endPoints = new DnsEndPoint[0];
            _serverSettings = new ServerSettings();
        }

        internal ClusterSettings(
            IClusterListener clusterListener,
            ClusterType clusterType,
            IReadOnlyList<DnsEndPoint> endPoints,
            IMessageListener messageListener,
            ServerSettings serverSettings)
        {
            _clusterListener = clusterListener;
            _clusterType = clusterType;
            _endPoints = endPoints;
            _messageListener = messageListener;
            _serverSettings = serverSettings;
        }

        public ClusterSettings(string uriString)
            : this(new Uri(Ensure.IsNotNull(uriString, "uriString")))
        {
        }

        public ClusterSettings(Uri uri)
        {
            var parsed = ClusterSettingsUriParser.Parse(uri);
            _clusterListener = parsed._clusterListener;
            _clusterType = parsed._clusterType;
            _endPoints = parsed._endPoints;
            _messageListener = parsed._messageListener;
            _serverSettings = parsed._serverSettings;
        }

        // properties
        public IClusterListener ClusterListener
        {
            get { return _clusterListener; }
        }

        public ClusterType ClusterType
        {
            get { return _clusterType; }
        }

        public IReadOnlyList<DnsEndPoint> EndPoints
        {
            get { return _endPoints; }
        }

        public IMessageListener MessageListener
        {
            get { return _messageListener; }
        }

        public ServerSettings ServerSettings
        {
            get { return _serverSettings; }
        }

        // methods
        public ClusterSettings WithClusterListener(IClusterListener value)
        {
            return object.ReferenceEquals(_clusterListener, value) ? this : new Builder(this) { _clusterListener = value }.Build();
        }

        public ClusterSettings WithClusterType(ClusterType value)
        {
            return (_clusterType == value) ? this : new Builder(this) { _clusterType = value }.Build();
        }

        public ClusterSettings WithEndPoints(IEnumerable<DnsEndPoint> value)
        {
            var list = value.ToList();
            return _endPoints.SequenceEqual(list) ? this : new Builder(this) { _endPoints = list }.Build();
        }

        public ClusterSettings WithMessageListener(IMessageListener value)
        {
            return object.ReferenceEquals(_messageListener, value) ? this : new Builder(this) { _messageListener = value }.Build();
        }

        public ClusterSettings WithServerSettings(ServerSettings value)
        {
            return object.Equals(_serverSettings, value) ? this : new Builder(this) { _serverSettings = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public IClusterListener _clusterListener;
            public ClusterType _clusterType;
            public IReadOnlyList<DnsEndPoint> _endPoints;
            public IMessageListener _messageListener;
            public ServerSettings _serverSettings;

            // constructors
            public Builder(ClusterSettings other)
            {
                _clusterListener = other.ClusterListener;
                _clusterType = other.ClusterType;
                _endPoints = other.EndPoints;
                _messageListener = other.MessageListener;
                _serverSettings = other.ServerSettings;
            }

            // methods
            public ClusterSettings Build()
            {
                return new ClusterSettings(
                    _clusterListener,
                    _clusterType,
                    _endPoints,
                    _messageListener,
                    _serverSettings);
            }
        }
    }
}
