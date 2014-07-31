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
using MongoDB.Shared;

namespace MongoDB.Driver.Core.Connections
{
    public sealed class ConnectionDescription : IEquatable<ConnectionDescription>
    {
        // fields
        private readonly BuildInfoResult _buildInfoResult;
        private readonly ConnectionId _connectionId;
        private readonly IsMasterResult _isMasterResult;
        private readonly int _maxBatchCount;
        private readonly int _maxDocumentSize;
        private readonly int _maxMessageSize;
        private readonly SemanticVersion _serverVersion;

        // constructors
        public ConnectionDescription(ConnectionId connectionId, IsMasterResult isMasterResult, BuildInfoResult buildInfoResult)
        {
            _connectionId = Ensure.IsNotNull(connectionId, "connectionId");
            _buildInfoResult = Ensure.IsNotNull(buildInfoResult, "buildInfoResult");
            _isMasterResult = Ensure.IsNotNull(isMasterResult, "isMasterResult");

            _maxBatchCount = isMasterResult.MaxBatchCount;
            _maxDocumentSize = isMasterResult.MaxDocumentSize;
            _maxMessageSize = isMasterResult.MaxMessageSize;
            _serverVersion = buildInfoResult.ServerVersion;
        }

        // properties
        public BuildInfoResult BuildInfoResult
        {
            get { return _buildInfoResult; }
        }

        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        public IsMasterResult IsMasterResult
        {
            get { return _isMasterResult; }
        }

        public int MaxBatchCount
        {
            get { return _maxBatchCount; }
        }

        public int MaxDocumentSize
        {
            get { return _maxDocumentSize; }
        }

        public int MaxMessageSize
        {
            get { return _maxMessageSize; }
        }

        public SemanticVersion ServerVersion
        {
            get { return _serverVersion; }
        }

        // methods
        public bool Equals(ConnectionDescription other)
        {
            if (other == null)
            {
                return false;
            }

            return
                _buildInfoResult.Equals(other._buildInfoResult) &&
                _connectionId.Equals(other._connectionId) &&
                _isMasterResult.Equals(other._isMasterResult);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConnectionDescription);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(_buildInfoResult)
                .Hash(_connectionId)
                .Hash(_isMasterResult)
                .GetHashCode();
        }
    }
}