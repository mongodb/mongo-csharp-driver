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
    [Serializable]
    public class MongoCredentials {
        #region private fields
        private string username;
        private string password;
        private bool admin;
        #endregion

        #region constructors
        public MongoCredentials(
            string username,
            string password
        ) {
            ValidatePassword(password);
            if (username.EndsWith("(admin)")) {
                this.username = username.Substring(0, username.Length - 7);
                this.password = password;
                this.admin = true;
            } else {
                this.username = username;
                this.password = password;
                this.admin = false;
            }
        }

        public MongoCredentials(
            string username,
            string password,
            bool admin
        ) {
            this.username = username;
            this.password = password;
            this.admin = admin;
        }
        #endregion

        #region factory methods
        public static MongoCredentials Create(
            string username,
            string password
        ) {
            if (username != null && password != null) {
                return new MongoCredentials(username, password);
            } else {
                return null;
            }
        }
        #endregion

        #region public properties
        public string Username {
            get { return username; }
        }

        public string Password {
            get { return password; }
        }

        public bool Admin {
            get { return admin; }
        }
        #endregion

        #region public operators
        public static bool operator ==(
            MongoCredentials lhs,
            MongoCredentials rhs
        ) {
            if (object.ReferenceEquals(lhs, rhs)) { return true; } // both null or same object
            if (object.ReferenceEquals(lhs, null) || object.ReferenceEquals(rhs, null)) { return false; }
            if (lhs.GetType() != rhs.GetType()) { return false; }
            return lhs.username == rhs.username && lhs.password == rhs.password && lhs.admin == rhs.admin;
        }

        public static bool operator !=(
            MongoCredentials lhs,
            MongoCredentials rhs
        ) {
            return !(lhs == rhs);
        }
        #endregion

        #region public static methods
        public static bool Equals(
            MongoCredentials lhs,
            MongoCredentials rhs
        ) {
            return lhs == rhs;
        }
        #endregion

        #region public methods
        public bool Equals(
            MongoCredentials rhs
        ) {
            return this == rhs;
        }

        public override bool Equals(object obj) {
            return this == obj as MongoCredentials; // works even if obj is null or of a different type
        }

        public override int GetHashCode() {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + username.GetHashCode();
            hash = 37 * hash + password.GetHashCode();
            hash = 37 * hash + admin.GetHashCode();
            return hash;
        }

        public override string ToString() {
            return string.Format("{0}{1}:{2}", username, admin ? "(admin)" : "", password);
        }
        #endregion

        #region private methods
        private void ValidatePassword(
            string password
        ) {
            if (password == null) {
                throw new ArgumentNullException("password");
            }
            if (password.Any(c => (int) c >= 128)) {
                throw new ArgumentException("Password must contain only ASCII characters");
            }
        }
        #endregion
    }
}
