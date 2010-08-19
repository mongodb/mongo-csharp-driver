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

namespace MongoDB.MongoDBClient {
    public class MongoConnectionStringBuilder {
        #region private fields
        private List<MongoServerAddress> addresses = new List<MongoServerAddress>();
        private string database;
        private string username;
        private string password;
        #endregion

        #region constructors
        public MongoConnectionStringBuilder() {
        }

        public MongoConnectionStringBuilder(
            string connectionString
        ) {
            ConnectionString = connectionString;
        }
        #endregion

        #region public properties
        public string ConnectionString {
            get { return ToString(); }
            set { Parse(value); }
        }

        public List<MongoServerAddress> Addresses {
            get { return addresses; }
            set { addresses = value; }
        }

        public string Database {
            get { return database; }
            set { database = value; }
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
            string connectionString
        ) {
            const string pattern =
                @"^mongodb://" +
                @"((?<username>[^:]+):(?<password>[^@]+)@)?" +
                @"(?<addresses>[^:,/]+(:\d+)?(,[^:,/]+(:\d+)?)*)" +
                @"(/(?<database>.+))?$";
            Match match = Regex.Match(connectionString, pattern);
            if (match.Success) {
                string username = match.Groups["username"].Value;
                string password = match.Groups["password"].Value;
                string addressStrings = match.Groups["addresses"].Value;
                string database = match.Groups["database"].Value;
                List<MongoServerAddress> addresses = new List<MongoServerAddress>();
                foreach (string addressString in addressStrings.Split(',')) {
                    match = Regex.Match(addressString, @"^(?<host>[^:]+)(:(?<port>\d+))?$");
                    if (match.Success) {
                        string host = match.Groups["host"].Value;
                        string port = match.Groups["port"].Value;
                        MongoServerAddress address = new MongoServerAddress(
                            host,
                            port == "" ? 27017 : int.Parse(port)
                        );
                        addresses.Add(address);
                    } else {
                        throw new ArgumentException("Invalid connection string");
                    }
                }

                this.addresses = addresses;
                this.database = database != "" ? database : null;
                this.username = username != "" ? username : null;
                this.password = password != "" ? password : null;
            } else {
                throw new ArgumentException("Invalid connection string");
            }
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
            foreach (MongoServerAddress address in addresses) {
                if (!first) { sb.Append(","); }
                sb.Append(address.Host);
                if (address.Port != 27017) {
                    sb.Append(":");
                    sb.Append(address.Port);
                }
                first = false;
            }
            if (database != null) {
                sb.Append("/");
                sb.Append(database);
            }
            return sb.ToString();
        }
        #endregion
    }
}
