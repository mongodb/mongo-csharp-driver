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
using System.Text.RegularExpressions;

using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoUrl {
        #region private fields
        private List<MongoServerAddress> seedList = new List<MongoServerAddress>();
        private string databaseName;
        private string username;
        private string password;
        #endregion

        #region constructors
        public MongoUrl() {
        }

        public MongoUrl(
            string urlString
        ) {
            Parse(urlString);
        }
        #endregion

        #region public properties
        public MongoServerAddress Address {
            get { return seedList.Single(); }
            set { seedList = new List<MongoServerAddress> { value }; }
        }

        public List<MongoServerAddress> SeedList {
            get { return seedList; }
            set { seedList = value; }
        }

        public string DatabaseName {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public string Username {
            get { return username; }
            set { username = value; }
        }

        public string Password {
            get { return password; }
            set { password = value; }
        }
        #endregion

        #region public methods
        public void Parse(
            string urlString
        ) {
            const string pattern =
                @"^mongodb://" +
                @"((?<username>[^:]+):(?<password>[^@]+)@)?" +
                @"(?<hosts>[^:,/]+(:\d+)?(,[^:,/]+(:\d+)?)*)" +
                @"(/(?<database>.+)?)?$";
            Match match = Regex.Match(urlString, pattern);
            if (match.Success) {
                string username = match.Groups["username"].Value;
                string password = match.Groups["password"].Value;
                string hosts = match.Groups["hosts"].Value;
                string databaseName = match.Groups["database"].Value;

                List<MongoServerAddress> seedList = new List<MongoServerAddress>();
                foreach (string host in hosts.Split(',')) {
                    MongoServerAddress address;
                    if (MongoServerAddress.TryParse(host, out address)) {
                        seedList.Add(address);
                    } else {
                        throw new ArgumentException("Invalid connection string");
                    }
                }

                this.seedList = seedList;
                this.databaseName = databaseName != "" ? databaseName : null;
                this.username = username != "" ? username : null;
                this.password = password != "" ? password : null;
            } else {
                throw new ArgumentException("Invalid connection string");
            }
        }

        public MongoConnectionSettings ToConnectionSettings() {
            return new MongoConnectionSettings {
                Credentials = MongoCredentials.Create(username, password),
                SeedList = seedList,
                DatabaseName = databaseName
            };
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("mongodb://");
            if (username != null && password != null) {
                sb.Append(username);
                sb.Append(":");
                sb.Append(password);
                sb.Append("@");
            }
            bool first = true;
            foreach (MongoServerAddress address in seedList) {
                if (!first) { sb.Append(","); }
                sb.Append(address.Host);
                if (address.Port != 27017) {
                    sb.Append(":");
                    sb.Append(address.Port);
                }
                first = false;
            }
            if (databaseName != null) {
                sb.Append("/");
                sb.Append(databaseName);
            }
            return sb.ToString();
        }
        #endregion
    }
}
