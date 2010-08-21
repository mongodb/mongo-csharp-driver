/* Copyright 2010 10gen Inc.
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

using MongoDB.BsonLibrary;
using MongoDB.MongoDBClient.Internal;

namespace MongoDB.MongoDBClient {
    public class MongoCollection {
        #region protected fields
        protected MongoDatabase database;
        protected string name;
        #endregion

        #region constructors
        public MongoCollection(
            MongoDatabase database,
            string name
        ) {
            this.database = database;
            this.name = name;
        }
        #endregion

        #region public properties
        public MongoDatabase Database {
            get { return database; }
        }

        public string FullName {
            get { return database.Name + "." + name; }
        }

        public string Name {
            get { return name; }
        }
        #endregion

        #region public methods
        public void CreateIndex(
            BsonDocument keys,
            BsonDocument options
        ) {
            throw new NotImplementedException();
        }

        public List<BsonDocument> Distinct(
            BsonDocument keys
        ) {
            return Distinct(keys, null);
        }

        public List<BsonDocument> Distinct(
            BsonDocument keys,
            BsonDocument query
        ) {
            throw new NotImplementedException();
        }

        public void DropIndex(
            BsonDocument keys
        ) {
            throw new NotImplementedException();
        }

        public void DropIndex(
            string name
        ) {
            throw new NotImplementedException();
        }

        public void DropIndexes() {
            DropIndex("*");
        }

        public MongoCursor<T> Find<T>(
            BsonDocument query
        ) where T : new() {
            return new MongoCursor<T>(this, query);
        }

        public MongoCursor<T> FindAll<T>() where T : new() {
            return new MongoCursor<T>(this, null);
        }

        public T FindOne<T>() where T : new() {
            return new MongoCursor<T>(this, null).Limit(1).FirstOrDefault();
        }

        public T FindOne<T>(
            BsonDocument query
        ) where T : new() {
            return new MongoCursor<T>(this, query).Limit(1).FirstOrDefault();
        }

        public int GetCount() {
            return GetCount(null);
        }

        public int GetCount(
            BsonDocument query
        ) {
            throw new NotImplementedException();
        }

        public List<BsonDocument> GetIndexInfo() {
            throw new NotImplementedException();
        }

        public MongoCommandResult GetStats() {
            throw new NotImplementedException();
        }

        public T Group<T>(
            BsonDocument keys,
            BsonDocument condition,
            BsonDocument initial,
            string reduce
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult Insert<T>(
            IEnumerable<T> documents
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult Insert<T>(
            params T[] documents
        ) {
            throw new NotImplementedException();
        }

        public bool IsCapped() {
            throw new NotImplementedException();
        }

        public MongoMapReduceResult MapReduce(
            BsonDocument query,
            string map,
            string reduce,
            string outputCollection
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult Remove(
            BsonDocument query
        ) {
            throw new NotImplementedException();
        }

        public void Rename(
            string name
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult Save<T>(
            T document
        ) {
            throw new NotImplementedException();
        }

        public override string  ToString() {
 	         return FullName;
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update
        ) {
            return Update<T>(query, update, false, false);
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update,
            bool upsert,
            bool multi
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult UpdateMulti<T>(
            BsonDocument query,
            T update
        ) {
            return Update<T>(query, update, false, true);
        }
        #endregion
    }

    public class MongoCollection<T> : MongoCollection where T : new() {
        #region constructors
        public MongoCollection(
            MongoDatabase database,
            string name
        )
            : base(database, name) {
        }
        #endregion

        #region public methods
        public MongoCursor<T> Find(
            BsonDocument query
        ) {
            return Find<T>(query);
        }

        public MongoCursor<T> FindAll() {
            return FindAll<T>();
        }

        public T FindOne() {
            return FindOne<T>();
        }

        public T FindOne(
            BsonDocument query
        ) {
            return FindOne<T>(query);
        }

        public MongoWriteResult Insert(
            IEnumerable<T> documents
        ) {
            return Insert<T>(documents);
        }

        public MongoWriteResult Insert(
            params T[] documents
        ) {
            return Insert<T>(documents);
        }

        public MongoWriteResult Save(
            T document
        ) {
            return Save<T>(document);
        }

        public MongoWriteResult Update(
            BsonDocument query,
            T update
        ) {
            return Update<T>(query, update, false, false);
        }

        public MongoWriteResult Update(
            BsonDocument query,
            T update,
            bool upsert,
            bool multi
        ) {
            return Update<T>(query, update, upsert, multi);
        }

        public MongoWriteResult UpdateMulti(
            BsonDocument query,
            T update
        ) {
            return Update<T>(query, update, false, true);
        }
        #endregion
    }
}
