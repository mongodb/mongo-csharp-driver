using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;
using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoWriteResult {
        #region private fields
        private MongoDatabase database;
        private MongoConnection connection;
        private int messageCounter;
        private BsonDocument lastError;
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
            BsonDocument lastError
        ) {
            this.lastError = lastError;
        }
        #endregion

        #region public methods
        public BsonDocument GetLastError() {
            if (lastError != null) {
                return lastError;
            }

            lastError = connection.TryGetLastError(database, messageCounter);
            return lastError;
        }
        #endregion
    }
}
