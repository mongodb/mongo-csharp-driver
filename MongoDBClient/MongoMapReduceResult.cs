using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;

namespace MongoDB.MongoDBClient {
    public class MongoMapReduceResult {
        #region private fields
        private MongoDatabase database;
        private string collectionName;
        private BsonDocument counts;
        #endregion

        #region constructors
        public MongoMapReduceResult(
            MongoDatabase database,
            string collectionName,
            BsonDocument counts
        ) {
            this.database = database;
            this.collectionName = collectionName;
            this.counts = counts;
        }
        #endregion

        #region public properties
        public MongoDatabase Database {
            get { return database; }
        }

        public string CollectionName {
            get { return collectionName; }
        }

        public BsonDocument Counts {
            get { return counts; }
        }
        #endregion

        #region public methods
        public MongoCursor<T> GetResults<T>() where T : new() {
            MongoCollection collection = database.GetCollection(collectionName);
            return collection.FindAll<T>();
        }
        #endregion
    }
}
