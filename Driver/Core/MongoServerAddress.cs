﻿/* Copyright 2010-2011 10gen Inc.
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
    /// <summary>
    /// The address of a MongoDB server.
    /// </summary>
    [Serializable]
    public class MongoServerAddress : IEquatable<MongoServerAddress> {
        #region private fields
        private string host;
        private int port;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of MongoServerAddress.
        /// </summary>
        /// <param name="host">The server's host name.</param>
        public MongoServerAddress(
           string host
       ) {
            this.host = host;
            this.port = 27017;
        }

        /// <summary>
        /// Initializes a new instance of MongoServerAddress.
        /// </summary>
        /// <param name="host">The server's host name.</param>
        /// <param name="port">The server's port number.</param>
        public MongoServerAddress(
            string host,
            int port
        ) {
            this.host = host;
            this.port = port;
        }
        #endregion

        #region factory methods
        /// <summary>
        /// Parses a string representation of a server address.
        /// </summary>
        /// <param name="value">The string representation of a server address.</param>
        /// <returns>A new instance of MongoServerAddress initialized with values parsed from the string.</returns>
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

        /// <summary>
        /// Tries to parse a string representation of a server address.
        /// </summary>
        /// <param name="value">The string representation of a server address.</param>
        /// <param name="address">The server address (set to null if TryParse fails).</param>
        /// <returns>True if the string is parsed succesfully.</returns>
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
        /// <summary>
        /// Gets the server's host name.
        /// </summary>
        public string Host {
            get { return host; }
        }

        /// <summary>
        /// Gets the server's port number.
        /// </summary>
        public int Port {
            get { return port; }
        }
        #endregion

        #region public operators
        /// <summary>
        /// Compares two server addresses.
        /// </summary>
        /// <param name="lhs">The first address.</param>
        /// <param name="rhs">The other address.</param>
        /// <returns>True if the two addresses are equal (or both are null).</returns>
        public static bool operator ==(
            MongoServerAddress lhs,
            MongoServerAddress rhs
        ) {
            if (object.ReferenceEquals(lhs, rhs)) { return true; } // both null or same object
            if (object.ReferenceEquals(lhs, null) || object.ReferenceEquals(rhs, null)) { return false; }
            if (lhs.GetType() != rhs.GetType()) { return false; }
            return lhs.host == rhs.host && lhs.port == rhs.port;
        }

        /// <summary>
        /// Compares two server addresses.
        /// </summary>
        /// <param name="lhs">The first address.</param>
        /// <param name="rhs">The other address.</param>
        /// <returns>True if the two addresses are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(
            MongoServerAddress lhs,
            MongoServerAddress rhs
        ) {
            return !(lhs == rhs);
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Compares two server addresses.
        /// </summary>
        /// <param name="lhs">The first server address.</param>
        /// <param name="rhs">The other server address.</param>
        /// <returns>True if the two server addresses are equal (or both are null).</returns>
        public static bool Equals(
            MongoServerAddress lhs,
            MongoServerAddress rhs
        ) {
            return lhs == rhs;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Compares two server addresses.
        /// </summary>
        /// <param name="rhs">The other server address.</param>
        /// <returns>True if the two server addresses are equal.</returns>
        public bool Equals(
            MongoServerAddress rhs
        ) {
            return this == rhs;
        }

        /// <summary>
        /// Compares two server addresses.
        /// </summary>
        /// <param name="obj">The other server address.</param>
        /// <returns>True if the two server addresses are equal.</returns>
        public override bool Equals(object obj) {
            return this == obj as MongoServerAddress; // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code for this object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + host.GetHashCode();
            hash = 37 * hash + port.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the server address.
        /// </summary>
        /// <returns>A string representation of the server address.</returns>
        public override string ToString() {
            return string.Format("{0}:{1}", host, port);
        }

        /// <summary>
        /// Returns the server address as an IPEndPoint (does a DNS lookup).
        /// </summary>
        /// <returns>The IPEndPoint of the server.</returns>
        public IPEndPoint ToIPEndPoint(
            AddressFamily addressFamily
        ) {
            var ipAddresses = Dns.GetHostAddresses(host);
            if (ipAddresses != null && ipAddresses.Length != 0) {
                foreach (var ipAddress in ipAddresses) {
                    if (ipAddress.AddressFamily == addressFamily) {
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
