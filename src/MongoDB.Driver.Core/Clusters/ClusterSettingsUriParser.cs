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
using System.Net;
using System.Net.Sockets;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Authentication.Credentials;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a parser for creating a ClusterSettings from a Uri.
    /// </summary>
    public class ClusterSettingsUriParser
    {
        #region static
        public static ClusterSettings Parse(string uriString)
        {
            return Parse(new Uri(uriString));
        }

        public static ClusterSettings Parse(Uri uri)
        {
            Ensure.IsNotNull(uri, "uri");
            return new ClusterSettingsUriParser(uri).Parse();
        }
        #endregion

        // fields
        private AddressFamily _addressFamily = AddressFamily.Unspecified;
        private ClusterType _clusterType = ClusterType.Standalone;
        private ICredential _credential = null;
        private List<DnsEndPoint> _endPoints = new List<DnsEndPoint>();
        private ServerSettings _serverSettings = new ServerSettings();
        private readonly Uri _uri;

        // constructors
        private ClusterSettingsUriParser(Uri uri)
        {
            _uri = uri;
        }

        // methods
        private ClusterSettings Parse()
        {
            ParseQuery();
            ParseEndPoint(); // ParseQuery first in case URI contains addressType
            ParseCredential();
            return new ClusterSettings(
                clusterListener: null,
                clusterType: _clusterType,
                endPoints: _endPoints,
                messageListener: null,
                serverSettings: _serverSettings);
        }

        private void ParseCredential()
        {
            var userInfo = _uri.UserInfo;
            if (userInfo != null && userInfo != "")
            {
                var parts = userInfo.Split(':');
                var username = parts[0];
                var password = parts[1];
                _credential = new UsernamePasswordCredential("admin", username, password);
            }
        }

        private void ParseEndPoint()
        {
            var port = (_uri.Port == -1) ? 27017 : _uri.Port;
            var endPoint = new DnsEndPoint(_uri.Host, port, _addressFamily);
            _endPoints.Insert(0, endPoint); // since we processed query first _endPoints might already contain some entries
        }

        private void ParseName(string name)
        {
            // will there be any names without a value?
        }

        private void ParseNameValue(string name, string value)
        {
            switch (name.ToLowerInvariant())
            {
                case "addressfamily":
                    switch (value.ToLowerInvariant())
                    {
                        case "ipv4":
                            _addressFamily = AddressFamily.InterNetwork;
                            break;
                        case "ipv6":
                            _addressFamily = AddressFamily.InterNetworkV6;
                            break;
                    }
                    break;
                case "clustertype":
                    ClusterType clusterType;
                    if (Enum.TryParse<ClusterType>(value, true, out clusterType))
                    {
                        _clusterType = clusterType;
                    }
                    break;
                case "endpoint":
                    DnsEndPoint endPoint;
                    if (DnsEndPointParser.TryParse(value, _addressFamily, out endPoint))
                    {
                        _endPoints.Add(endPoint);
                    }
                    break;
                case "heartbeat":
                case "heartbeatinterval":
                    TimeSpan heartbeatInterval;
                    if (TimeSpanParser.TryParse(value, out heartbeatInterval))
                    {
                        _serverSettings = _serverSettings.WithHeartbeatInterval(heartbeatInterval);
                    }
                    break;
            }
        }

        private void ParseQuery()
        {
            var query = _uri.Query;
            if (query != null)
            {
                if (query.StartsWith("?"))
                {
                    query = query.Substring(1);
                }

                if (query != "")
                {
                    foreach (var pair in query.Split(';', '&'))
                    {
                        var parts = pair.Split('=');
                        switch (parts.Length)
                        {
                            case 1:
                                ParseName(parts[0]);
                                break;
                            case 2:
                                ParseNameValue(parts[0], parts[1]);
                                break;
                        }
                    }
                }
            }
        }
    }
}
