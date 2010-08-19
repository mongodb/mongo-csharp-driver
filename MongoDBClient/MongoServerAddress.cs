using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.MongoDBClient {
    public class MongoServerAddress {
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

        #region public properties
        public string Host {
            get { return host; }
        }

        public int Port {
            get { return port; }
        }
        #endregion

        // TODO: implement GetHashcode, Equals, etc...
    }
}
