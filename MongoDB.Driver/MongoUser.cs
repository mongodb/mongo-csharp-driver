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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB user.
    /// </summary>
    [Serializable]
    public class MongoUser : IEquatable<MongoUser>
    {
        // private fields
        private string _username;
        private string _passwordHash;
        private bool _isReadOnly;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoUser.
        /// </summary>
        /// <param name="credentials">The user's credentials.</param>
        /// <param name="isReadOnly">Whether the user has read-only access.</param>
        public MongoUser(MongoCredentials credentials, bool isReadOnly)
        {
            if (credentials == null)
            {
                throw new ArgumentNullException("credentials");
            }
            _username = credentials.Username;
            _passwordHash = HashPassword(credentials.Username, credentials.Password);
            _isReadOnly = isReadOnly;
        }

        /// <summary>
        /// Creates a new instance of MongoUser.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="passwordHash">The password hash.</param>
        /// <param name="isReadOnly">Whether the user has read-only access.</param>
        public MongoUser(string username, string passwordHash, bool isReadOnly)
        {
            if (username == null)
            {
                throw new ArgumentNullException("username");
            }
            if (passwordHash == null)
            {
                throw new ArgumentNullException("passwordHash");
            }
            _username = username;
            _passwordHash = passwordHash;
            _isReadOnly = isReadOnly;
        }

        // public properties
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username
        {
            get { return _username; }
            set {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _username = value;
            }
        }

        /// <summary>
        /// Gets or sets the password hash.
        /// </summary>
        public string PasswordHash
        {
            get { return _passwordHash; }
            set {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _passwordHash = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the user is a read-only user.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; }
        }

        // public operators
        /// <summary>
        /// Determines whether two specified MongoUser objects have different values.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is different from the value of rhs; otherwise, false.</returns>
        public static bool operator !=(MongoUser lhs, MongoUser rhs)
        {
            return !MongoUser.Equals(lhs, rhs);
        }

        /// <summary>
        /// Determines whether two specified MongoUser objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool operator ==(MongoUser lhs, MongoUser rhs)
        {
            return MongoUser.Equals(lhs, rhs);
        }

        // public static methods
        /// <summary>
        /// Determines whether two specified MongoUser objects have the same value.
        /// </summary>
        /// <param name="lhs">The first value to compare, or null.</param>
        /// <param name="rhs">The second value to compare, or null.</param>
        /// <returns>True if the value of lhs is the same as the value of rhs; otherwise, false.</returns>
        public static bool Equals(MongoUser lhs, MongoUser rhs)
        {
            if ((object)lhs == null) { return (object)rhs == null; }
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Calculates the password hash.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The password hash.</returns>
        public static string HashPassword(string username, string password)
        {
            return MongoUtils.Hash(username + ":mongo:" + password);
        }

        // public methods
        /// <summary>
        /// Determines whether this instance and another specified MongoUser object have the same value.
        /// </summary>
        /// <param name="rhs">The MongoUser object to compare to this instance.</param>
        /// <returns>True if the value of the rhs parameter is the same as this instance; otherwise, false.</returns>
        public bool Equals(MongoUser rhs)
        {
            if ((object)rhs == null || GetType() != rhs.GetType()) { return false; }
            if ((object)this == (object)rhs) { return true; }
            return _username.Equals(rhs._username) && _passwordHash.Equals(rhs._passwordHash) && _isReadOnly.Equals(rhs._isReadOnly);
        }

        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a MongoUser object, have the same value.
        /// </summary>
        /// <param name="obj">The MongoUser object to compare to this instance.</param>
        /// <returns>True if obj is a MongoUser object and its value is the same as this instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoUser); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Returns the hash code for this Class1 object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _username.GetHashCode();
            hash = 37 * hash + _passwordHash.GetHashCode();
            hash = 37 * hash + _isReadOnly.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the credentials.
        /// </summary>
        /// <returns>A string representation of the user.</returns>
        public override string ToString()
        {
            return string.Format("User:{0}", _username);
        }
    }
}
