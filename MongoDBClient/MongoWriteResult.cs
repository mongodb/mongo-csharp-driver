using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoWriteResult {
        #region private fields
        private MongoDatabase database;
        private MongoConnection connection;
        private int messageCounter;
        private MongoCommandResult lastError;
        #endregion

        #region constructors
        internal MongoWriteResult(
            MongoDatabase database,
            MongoConnection connection
        ) {
            this.database = database;
            this.connection = connection;
            messageCounter = connection.MessageCounter;
        }

        public MongoWriteResult(
            MongoCommandResult lastError
        ) {
            this.lastError = lastError;
        }
        #endregion

        #region public methods
        public MongoCommandResult GetLastError() {
            if (lastError != null) {
                return lastError;
            }

            lastError = connection.TryGetLastError(database, messageCounter);
            return lastError;
        }
        #endregion
    }
}
