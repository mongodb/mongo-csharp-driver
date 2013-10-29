﻿/* Copyright 2010-2013 10gen Inc.
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

using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.FeatureDetection
{
    internal class FeatureContext
    {
        // private fields
        private MongoServerBuildInfo _buildInfo;
        private MongoConnection _connection;
        private IsMasterResult _isMasterResult;
        private MongoServerInstance _serverInstance;

        // public methods
        public MongoServerBuildInfo BuildInfo
        {
            get { return _buildInfo; }
            set { _buildInfo = value; }
        }

        public MongoConnection Connection
        {
            get { return _connection; }
            set { _connection = value; }
        }

        public IsMasterResult IsMasterResult
        {
            get { return _isMasterResult; }
            set { _isMasterResult = value; }
        }

        public MongoServerInstance ServerInstance
        {
            get { return _serverInstance; }
            set { _serverInstance = value; }
        }
    }
}
