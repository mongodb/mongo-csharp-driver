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
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoConnectionStringBuilder : DbConnectionStringBuilder {
        #region constructors
        public MongoConnectionStringBuilder(
            string connectionString
        )
            : base() {
            ConnectionString = connectionString;
        }
        #endregion

        #region public properties
        public List<MongoServerAddress> Addresses {
            get {
                string servers = Servers;
                if (servers == null) {
                    return null;
                } else {
                    List<MongoServerAddress> addresses = new List<MongoServerAddress>();
                    foreach (string server in servers.Split(',')) {
                        MongoServerAddress address = MongoServerAddress.Parse(server);
                        addresses.Add(address);
                    }
                    return addresses;
                }
            }
            set {
                StringBuilder builder = new StringBuilder();
                bool first = true;
                foreach (MongoServerAddress address in value) {
                    if (!first) { builder.Append(","); }
                    builder.Append(address.ToString());
                    first = false;
                }
                Servers = builder.ToString();
            }
        }

        public string Server {
            get { return GetItem("Server"); }
            set { this["Server"] = value; }
        }

        // a synonym for Server
        public string Servers {
            get { return Server; }
            set { Server = value; }
        }

        public string DatabaseName {
            get { return GetItem("Database"); }
            set { this["Database"] = value; }
        }

        public string Username {
            get { return GetItem("Username"); }
            set { this["Username"] = value; }
        }

        public string Password {
            get { return GetItem("Password"); }
            set { this["Password"] = value; }
        }
        #endregion

        #region public methods
        public new void Add(
            string key,
            object value
        ) {
            // normalize key name
            switch (key.ToLower()) {
                case "server": key = "Server"; break;
                case "servers": key = "Server"; break;
                case "database": key = "Database"; break;
                case "username": key = "Username"; break;
                case "password": key = "Password"; break;
                default: throw new ArgumentException("Invalid key");
            }
            base.Add(key, value);
        }

        public MongoConnectionSettings ToConnectionSettings() {
            return new MongoConnectionSettings {
                Addresses = Addresses,
                DatabaseName = DatabaseName,
                Username = Username,
                Password = Password
            };
        }
        #endregion

        #region private methods
        private string GetItem(
            string key
        ) {
            if (ContainsKey(key)) {
                return (string) this[key];
            } else {
                return null;
            }
        }
        #endregion
    }
}
