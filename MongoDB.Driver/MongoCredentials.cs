/* Copyright 2010-2013 10gen Inc.
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
using System.Linq;

namespace MongoDB.Driver
{
    /// <summary>
    /// Credentials to access a MongoDB database.
    /// </summary>
    [Serializable]
    public class MongoCredentials : IEquatable<MongoCredentials>
    {
        // private fields
        private string _username;
        private string _password;
        private bool _admin;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoCredentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public MongoCredentials(string username, string password)
        {
            ValidatePassword(password);
            if (username.EndsWith("(admin)", StringComparison.Ordinal))
            {
                _username = username.Substring(0, username.Length - 7);
                _password = password;
                _admin = true;
            }
            else
            {
                _username = username;
                _password = password;
                _admin = false;
            }
        }

        /// <summary>
        /// Creates a new instance of MongoCredentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="admin">Whether the credentials should be validated against the admin database.</param>
        public MongoCredentials(string username, string password, bool admin)
        {
            ValidatePassword(password);
            _username = username;
            _password = password;
            _admin = admin;
        }

        /// <summary>
        /// Creates an instance of MongoCredentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A new instance of MongoCredentials (or null if either parameter is null).</returns>
        // factory methods
        public static MongoCredentials Create(string username, string password)
        {
            if (username != null && password != null)
            {
                return new MongoCredentials(username, password);
            }
            else
            {
                return null;
            }
        }

        // public properties
        /// <summary>
        /// Gets the username.
        /// </summary>
        public string Username
        {
            get { return _username; }
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password
        {
            get { return _password; }
        }

        /// <summary>
        /// Gets whether the credentials should be validated against the admin database.
        /// </summary>
        public bool Admin
        {
            get { return _admin; }
        }

        // public operators
        /// <summary>
        /// Compares two MongoCredentials.
        /// </summary>
        /// <param name="lhs">The first MongoCredentials.</param>
        /// <param name="rhs">The other MongoCredentials.</param>
        /// <returns>True if the two MongoCredentials are equal (or both null).</returns>
        public static bool operator ==(MongoCredentials lhs, MongoCredentials rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two MongoCredentials.
        /// </summary>
        /// <param name="lhs">The first MongoCredentials.</param>
        /// <param name="rhs">The other MongoCredentials.</param>
        /// <returns>True if the two MongoCredentials are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoCredentials lhs, MongoCredentials rhs)
        {
            return !(lhs == rhs);
        }

        // public methods
        /// <summary>
        /// Compares this MongoCredentials to another MongoCredentials.
        /// </summary>
        /// <param name="rhs">The other credentials.</param>
        /// <returns>True if the two credentials are equal.</returns>
        public bool Equals(MongoCredentials rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return _username == rhs._username && _password == rhs._password && _admin == rhs._admin;
        }

        /// <summary>
        /// Compares this MongoCredentials to another MongoCredentials.
        /// </summary>
        /// <param name="obj">The other credentials.</param>
        /// <returns>True if the two credentials are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoCredentials); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hashcode for the credentials.
        /// </summary>
        /// <returns>The hashcode.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _username.GetHashCode();
            hash = 37 * hash + _password.GetHashCode();
            hash = 37 * hash + _admin.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the credentials.
        /// </summary>
        /// <returns>A string representation of the credentials.</returns>
        public override string ToString()
        {
            return string.Format("{0}{1}:{2}", _username, _admin ? "(admin)" : "", _password);
        }

        // private methods
        private void ValidatePassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            if (password.Any(c => (int)c >= 128))
            {
                throw new ArgumentException("Password must contain only ASCII characters.");
            }
        }
    }
}
