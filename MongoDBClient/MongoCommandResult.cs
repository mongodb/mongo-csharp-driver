using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient {
    public class MongoCommandResult {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public MongoCommandResult(
            BsonDocument document
        ) {
            this.document = document;
        }
        #endregion

        #region public properties
        public BsonDocument Document {
            get { return document; }
        }

        public string ErrorMessage {
            get {
                object message = document["errmsg"];
                if (message == null) {
                    return null;
                } else {
                    return message.ToString();
                }
            }
        }

        public bool OK {
            get {
                object ok = document["ok"];
                if (ok == null) {
                    throw new MongoException("ok element is missing");
                }
                if (ok is bool) {
                    return (bool) ok;
                } else if (ok is int) {
                    return (int) ok == 1;
                } else {
                    throw new MongoException("Unexpected value for ok element");
                }
            }
        }
        #endregion
    }
}
