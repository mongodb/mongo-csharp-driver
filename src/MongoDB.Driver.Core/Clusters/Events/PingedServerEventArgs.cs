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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters.Events
{
    public class PingedServerEventArgs
    {
        // fields
        private readonly BuildInfoResult _buildInfoResult;
        private readonly IsMasterResult _isMasterResult;
        private readonly DnsEndPoint _endPoint;
        private readonly TimeSpan _pingTime;

        // constructors
        public PingedServerEventArgs(DnsEndPoint endPoint, TimeSpan pingTime, IsMasterResult isMasterResult, BuildInfoResult buildInfoResult)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _pingTime = pingTime;
            _isMasterResult = isMasterResult; // can be null
            _buildInfoResult = buildInfoResult; // can be null
        }

        // properties
        public BuildInfoResult BuildInfoResult
        {
            get { return _buildInfoResult; }
        }

        public DnsEndPoint EndPoint
        {
            get { return _endPoint; }
        }

        public IsMasterResult IsMasterResult
        {
            get { return _isMasterResult; }
        }

        public TimeSpan PingTime
        {
            get { return _pingTime; }
        }
    }
}
