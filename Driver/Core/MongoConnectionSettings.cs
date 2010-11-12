/* Copyright 2010 10gen Inc.
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

namespace MongoDB.Driver {
    public enum ConnectionMode {
        Direct,
        ReplicaSet
    }

    [Serializable]
    public class MongoConnectionSettings {
        #region private fields
        private ConnectionMode connectionMode;
        private MongoCredentials credentials;
        private string databaseName;
        private string replicaSetName;
        private SafeMode safeMode;
        private IEnumerable<MongoServerAddress> servers;
        private bool slaveOk;
        #endregion

        #region constructors
        public MongoConnectionSettings() {
        }
        #endregion

        #region public properties
        public ConnectionMode ConnectionMode {
            get { return connectionMode; }
            set { connectionMode = value; }
        }

        public MongoCredentials Credentials {
            get { return credentials; }
            set { credentials = value; }
        }

        public string DatabaseName {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public string ReplicaSetName {
            get { return replicaSetName; }
            set { replicaSetName = value; }
        }

        public SafeMode SafeMode {
            get { return safeMode; }
            set { safeMode = value; }
        }

        public MongoServerAddress Server {
            get { return (servers == null) ? null : servers.Single(); }
            set { servers = new MongoServerAddress[] { value }; }
        }

        public IEnumerable<MongoServerAddress> Servers {
            get { return servers; }
            set { servers = value; }
        }

        public bool SlaveOk {
            get { return slaveOk; }
            set { slaveOk = value; }
        }
        #endregion
    }
}
