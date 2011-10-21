/* Copyright 2010-2011 10gen Inc.
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

namespace MongoDB.Driver {
    /// <summary>
    /// Represents a MongoDB user.
    /// </summary>
    [Serializable]
    public class MongoUser {
        #region private fields
        private string username;
        private string passwordHash;
        private bool isReadOnly;
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new instance of MongoUser.
        /// </summary>
        /// <param name="credentials">The user's credentials.</param>
        /// <param name="isReadOnly">Whether the user has read-only access.</param>
        public MongoUser(
            MongoCredentials credentials,
            bool isReadOnly
        ) {
            this.username = credentials.Username;
            this.passwordHash = HashPassword(credentials.Username, credentials.Password);
            this.isReadOnly = isReadOnly;
        }

        /// <summary>
        /// Creates a new instance of MongoUser.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="passwordHash">The password hash.</param>
        /// <param name="isReadOnly">Whether the user has read-only access.</param>
        public MongoUser(
            string username,
            string passwordHash,
            bool isReadOnly
        ) {
            this.username = username;
            this.passwordHash = passwordHash;
            this.isReadOnly = isReadOnly;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username {
            get { return username; }
            set { username = value; }
        }

        /// <summary>
        /// Gets or sets the password hash.
        /// </summary>
        public string PasswordHash {
            get { return passwordHash; }
            set { passwordHash = value; }
        }

        /// <summary>
        /// Gets or sets whether the user is a read-only user.
        /// </summary>
        public bool IsReadOnly {
            get { return isReadOnly; }
            set { isReadOnly = value; }
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Calculates the password hash.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The password hash.</returns>
        public static string HashPassword(
            string username,
            string password
        ) {
            return MongoUtils.Hash(username + ":mongo:" + password);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Returns a string representation of the credentials.
        /// </summary>
        /// <returns>A string representation of the user.</returns>
        public override string ToString() {
            return string.Format("User:{0}", username);
        }
        #endregion
    }
}
