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

namespace MongoDB.CSharpDriver.Internal {
    public class MongoConnectionSettings {
        #region private fields
        private MongoCredentials credentials;
        private string databaseName;
        private IEnumerable<MongoServerAddress> seedList;
        #endregion

        #region constructors
        public MongoConnectionSettings() {
        }
        #endregion

        #region public properties
        public MongoServerAddress Address {
            get { return seedList.First(); }
            set { seedList = new MongoServerAddress[] { value }; }
        }

        public MongoCredentials Credentials {
            get { return credentials; }
            set { credentials = value; }
        }

        public string DatabaseName {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public IEnumerable<MongoServerAddress> SeedList {
            get { return seedList; }
            set { seedList = value; }
        }
        #endregion
    }
}
