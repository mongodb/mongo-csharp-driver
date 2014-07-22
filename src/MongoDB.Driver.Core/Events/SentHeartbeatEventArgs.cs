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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    public class SentHeartbeatEventArgs
    {
        // fields
        private readonly BuildInfoResult _buildInfoResult;
        private readonly DnsEndPoint _endPoint;
        private readonly Exception _exception;
        private readonly IsMasterResult _isMasterResult;

        // constructors
        public SentHeartbeatEventArgs(DnsEndPoint endPoint, IsMasterResult isMasterResult, BuildInfoResult buildInfoResult)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _isMasterResult = Ensure.IsNotNull(isMasterResult, "isMasterResult");
            _buildInfoResult = Ensure.IsNotNull(buildInfoResult, "buildInfoResult");
        }

        public SentHeartbeatEventArgs(DnsEndPoint endPoint, Exception exception)
        {
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            _exception = Ensure.IsNotNull(exception, "exception");
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

        public Exception Exception
        {
            get { return _exception; }
        }

        public IsMasterResult IsMasterResult
        {
            get { return _isMasterResult; }
        }
    }
}
