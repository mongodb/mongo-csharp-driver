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
    /// Represents a MongoDB user.
    /// </summary>
    [Serializable]
    public class MongoUser
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
            set { _username = value; }
        }

        /// <summary>
        /// Gets or sets the password hash.
        /// </summary>
        public string PasswordHash
        {
            get { return _passwordHash; }
            set { _passwordHash = value; }
        }

        /// <summary>
        /// Gets or sets whether the user is a read-only user.
        /// </summary>
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; }
        }

        // public static methods
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
        /// Returns a string representation of the credentials.
        /// </summary>
        /// <returns>A string representation of the user.</returns>
        public override string ToString()
        {
            return string.Format("User:{0}", _username);
        }
    }
}
