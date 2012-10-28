/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a credentials store that contains credentials for different databases.
    /// </summary>
    public class MongoCredentialsStore
    {
        // private fields
        private Dictionary<string, MongoCredentials> _credentialsStore = new Dictionary<string,MongoCredentials>();
        private int _frozenHashCode;
        private string _frozenStringRepresentation;
        private bool _isFrozen;

        // constructors
        /// <summary>
        /// Creates a new instance of the MongoCredentialsStore class.
        /// </summary>
        public MongoCredentialsStore()
        {
        }

        // public methods
        /// <summary>
        /// Adds the credentials for a database to the store.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="credentials">The credentials.</param>
        public void AddCredentials(string databaseName, MongoCredentials credentials)
        {
            if (_isFrozen) { throw new InvalidOperationException("MongoCredentialsStore is frozen."); }
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }
            if (databaseName == "admin" && !credentials.Admin)
            {
                throw new ArgumentOutOfRangeException("credentials", "Credentials for the admin database must have the admin flag set to true.");
            }
            _credentialsStore.Add(databaseName, credentials);
        }

        /// <summary>
        /// Creates a clone of the credentials store.
        /// </summary>
        /// <returns>A clone of the credentials store.</returns>
        public MongoCredentialsStore Clone()
        {
            var clone = new MongoCredentialsStore();
            foreach (var item in _credentialsStore)
            {
                clone.AddCredentials(item.Key, item.Value);
            }
            return clone;
        }

        /// <summary>
        /// Compares this credentials store to another credentials store.
        /// </summary>
        /// <param name="rhs">The other credentials store.</param>
        /// <returns>True if the two credentials stores are equal.</returns>
        public bool Equals(MongoCredentialsStore rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) {
                return false;
            }

            if (_credentialsStore.Count != rhs._credentialsStore.Count)
            {
                return false;
            }

            foreach (var key in _credentialsStore.Keys)
            {
                if (!rhs._credentialsStore.ContainsKey(key))
                {
                    return false;
                }
                if (!_credentialsStore[key].Equals(rhs._credentialsStore[key]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compares this credentials store to another credentials store.
        /// </summary>
        /// <param name="obj">The other credentials store.</param>
        /// <returns>True if the two credentials stores are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoCredentialsStore); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Freezes the credentials store.
        /// </summary>
        /// <returns>The frozen credentials store.</returns>
        public MongoCredentialsStore Freeze()
        {
            if (!_isFrozen)
            {
                _frozenHashCode = GetHashCode();
                _frozenStringRepresentation = ToString();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Gets the hashcode for the credentials store.
        /// </summary>
        /// <returns>The hashcode.</returns>
        public override int GetHashCode()
        {
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            // see Effective Java by Joshua Bloch
            int hash = 17;
            var keys = _credentialsStore.Keys.ToArray();
            Array.Sort(keys);
            foreach (var key in keys)
            {
                hash = 37 * hash + key.GetHashCode();
                hash = 37 * hash + _credentialsStore[key].GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the credentials store.
        /// </summary>
        /// <returns>A string representation of the credentials store.</returns>
        public override string ToString()
        {
            if (_isFrozen)
            {
                return _frozenStringRepresentation;
            }

            var sb = new StringBuilder();
            sb.Append("{");
            var separator = "";
            var keys = _credentialsStore.Keys.ToArray();
            Array.Sort(keys);
            foreach (var key in keys)
            {
                var credentials = _credentialsStore[key];
                sb.Append(separator);
                sb.AppendFormat("{0}@{1}", credentials.ToString(), key);
                separator = ",";
            }
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the credentials for a database.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <param name="credentials">The credentials.</param>
        /// <returns>True if the store contained credentials for the database. Otherwise false.</returns>
        public bool TryGetCredentials(string databaseName, out MongoCredentials credentials)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }
            return _credentialsStore.TryGetValue(databaseName, out credentials);
        }
    }
}
