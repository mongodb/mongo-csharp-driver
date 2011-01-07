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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MongoDB.Driver {
    [Serializable]
    public class MongoServerAddress : IEquatable<MongoServerAddress> {
        #region private fields
        private string host;
        private int port;
        #endregion

        #region constructors
        public MongoServerAddress(
           string host
       ) {
            this.host = host;
            this.port = 27017;
        }

        public MongoServerAddress(
            string host,
            int port
        ) {
            this.host = host;
            this.port = port;
        }
        #endregion

        #region factory methods
        public static MongoServerAddress Parse(
            string value
        ) {
            MongoServerAddress address;
            if (TryParse(value, out address)) {
                return address;
            } else {
                throw new FormatException("Invalid server address");
            }
        }

        public static bool TryParse(
            string value,
            out MongoServerAddress address
        ) {
            address = null;

            Match match = Regex.Match(value, @"^(?<host>[^:]+)(:(?<port>\d+))?$");
            if (match.Success) {
                string host = match.Groups["host"].Value;
                string portString = match.Groups["port"].Value;
                int port = (portString == "") ? 27017 : XmlConvert.ToInt32(portString);
                address = new MongoServerAddress(host, port);
                return true;

            } else {
                return false;
            }
        }
        #endregion

        #region public properties
        public string Host {
            get { return host; }
        }

        public int Port {
            get { return port; }
        }
        #endregion

        #region public operators
        public static bool operator ==(
            MongoServerAddress lhs,
            MongoServerAddress rhs
        ) {
            if (object.ReferenceEquals(lhs, rhs)) { return true; } // both null or same object
            if (object.ReferenceEquals(lhs, null) || object.ReferenceEquals(rhs, null)) { return false; }
            if (lhs.GetType() != rhs.GetType()) { return false; }
            return lhs.host == rhs.host && lhs.port == rhs.port;
        }

        public static bool operator !=(
            MongoServerAddress lhs,
            MongoServerAddress rhs
        ) {
            return !(lhs == rhs);
        }
        #endregion

        #region public static methods
        public static bool Equals(
            MongoServerAddress lhs,
            MongoServerAddress rhs
        ) {
            return lhs == rhs;
        }
        #endregion

        #region public methods
        public bool Equals(
            MongoServerAddress rhs
        ) {
            return this == rhs;
        }

        public override bool Equals(object obj) {
            return this == obj as MongoServerAddress; // works even if obj is null or of a different type
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + host.GetHashCode();
            hash = 37 * hash + port.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return string.Format("{0}:{1}", host, port);
        }

        public IPEndPoint ToIPEndPoint() {
            var ipAddresses = Dns.GetHostAddresses(host);
            if (ipAddresses != null && ipAddresses.Length != 0) {
                foreach (var ipAddress in ipAddresses) {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork) {
                        return new IPEndPoint(ipAddress, port);
                    }
                }
            }
            var message = string.Format("Unable to resolve host name: {0}", host);
            throw new MongoConnectionException(message);
        }
        #endregion
    }
}
