using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.MongoDBClient {
    public class MongoCredentials {
        #region private fields
        private string username;
        private string password;
        #endregion

        #region constructors
        public MongoCredentials(
            string username,
            string password
        ) {
            this.username = username;
            this.password = password;
        }
        #endregion

        #region public properties
        public string Username {
            get { return username; }
        }

        public string Password {
            get { return password; }
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
            return lhs.username == rhs.username && lhs.password == rhs.password;
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
            return hash;
        }

        public override string ToString() {
            return string.Format("{0}:{1}", username, password);
        }
        #endregion
    }
}
