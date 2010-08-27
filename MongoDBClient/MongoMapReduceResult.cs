using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient {
    public class MongoMapReduceResult {
        #region private fields
        private MongoDatabase database;
        private BsonDocument commandResult;
        #endregion

        #region constructors
        public MongoMapReduceResult(
            MongoDatabase database,
            BsonDocument commandResult
        ) {
            this.database = database;
            this.commandResult = commandResult;
        }
        #endregion

        #region public properties
        public BsonDocument CommandResult {
            get { return commandResult; }
        }

        public BsonDocument Counts {
            get { return commandResult.GetEmbeddedDocument("counts"); }
        }

        public MongoDatabase Database {
            get { return database; }
        }

        public TimeSpan Duration {
            get { return TimeSpan.FromMilliseconds(commandResult.GetDouble("timeMillis")); }
        }

        public string ResultCollectionName {
            get { return commandResult.GetString("result"); }
        }
        #endregion

        #region public methods
        public MongoCursor<T> GetResults<T>() where T : new() {
            MongoCollection collection = database.GetCollection(ResultCollectionName);
            return collection.FindAll<T>();
        }
        #endregion
    }
}
