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
    }
}
