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
            get { return commandResult["counts"].AsBsonDocument; }
        }

        public MongoDatabase Database {
            get { return database; }
        }

        public TimeSpan Duration {
            get { return TimeSpan.FromMilliseconds(commandResult["timeMillis"].AsDouble); }
        }

        public string ResultCollectionName {
            get { return commandResult["result"].AsString; }
        }
        #endregion

        #region public methods
        public MongoCursor<R> GetResults<R>() where R : new() {
            MongoCollection<R> collection = database.GetCollection<R>(ResultCollectionName);
            return collection.FindAll();
        }
        #endregion
    }
}
