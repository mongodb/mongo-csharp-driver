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
using System.Collections;
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
        protected bool safeMode;
        #endregion

        #region constructors
        public MongoCollection(
            MongoDatabase database,
            string name
        ) {
            this.database = database;
            this.name = name;
            this.safeMode = database.SafeMode;
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

        public bool SafeMode {
            get { return safeMode; }
            set { safeMode = value; }
        }
        #endregion

        #region public methods
        public int Count() {
            return Count(null);
        }

        public int Count(
            BsonDocument query
        ) {
            BsonDocument command = new BsonDocument {
                { "count", name },
                { "query", query ?? new BsonDocument() }
            };
            var result = database.RunCommand(command);
            return (int) result.GetDouble("n");
        }

        public void CreateIndex(
            BsonDocument keys,
            BsonDocument options
        ) {
            throw new NotImplementedException();
        }

        // TODO: any arguments?
        public long DataSize() {
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
            string indexName
        ) {
            throw new NotImplementedException();
        }

        public void DropIndexes() {
            DropIndex("*");
        }

        public void EnsureIndex(
            BsonDocument keys,
            BsonDocument options
        ) {
            throw new NotImplementedException();
        }

        public MongoCursor<T> Find<T>(
            BsonDocument query
        ) where T : new() {
            return new MongoCursor<T>(this, query);
        }

        public MongoCursor<T> Find<T>(
            BsonDocument query,
            BsonDocument fields
        ) where T : new() {
            return new MongoCursor<T>(this, query, fields);
        }

        public MongoCursor<T> Find<T>(
            string where
        ) where T : new() {
            BsonDocument query = new BsonDocument {
                { "$where", new BsonJavaScriptCode(where) }
            };
            return new MongoCursor<T>(this, query);
        }

        public MongoCursor<T> Find<T>(
            string where,
            BsonDocument fields
        ) where T : new() {
            BsonDocument query = new BsonDocument {
                { "$where", new BsonJavaScriptCode(where) }
            };
            return new MongoCursor<T>(this, query, fields);
        }

        public MongoCursor<T> FindAll<T>() where T : new() {
            return new MongoCursor<T>(this, null);
        }

        public MongoCursor<T> FindAll<T>(
            BsonDocument fields
        ) where T : new() {
            return new MongoCursor<T>(this, null, fields);
        }

        public void FindAndModify() {
            throw new NotImplementedException();
        }

        public T FindOne<T>() where T : new() {
            using (var cursor = new MongoCursor<T>(this, null).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            BsonDocument query
        ) where T : new() {
            using (var cursor = new MongoCursor<T>(this, query).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            BsonDocument query,
            BsonDocument fields
        ) where T : new() {
            using (var cursor = new MongoCursor<T>(this, query, fields).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            string where
        ) where T : new() {
            BsonDocument query = new BsonDocument {
                { "$where", new BsonJavaScriptCode(where) }
            };
            using (var cursor = new MongoCursor<T>(this, query).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        public T FindOne<T>(
            string where,
            BsonDocument fields
        ) where T : new() {
            BsonDocument query = new BsonDocument {
                { "$where", new BsonJavaScriptCode(where) }
            };
            using (var cursor = new MongoCursor<T>(this, query, fields).Limit(1)) {
                return cursor.FirstOrDefault();
            }
        }

        // TODO: same as mongo shell's getIndexes?
        public List<BsonDocument> GetIndexInfo() {
            throw new NotImplementedException();
        }

        public BsonDocument GetStats() {
            throw new NotImplementedException();
        }

        // TODO: order of arguments is different in mongo shell!
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
            return Insert(documents, safeMode);
        }

        public MongoWriteResult Insert<T>(
            IEnumerable<T> documents,
            bool safeMode
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult Insert<T>(
            params T[] documents
        ) {
            return Insert((IEnumerable<T>) documents, safeMode);
        }

        public MongoWriteResult Insert<T>(
            T document,
            bool safeMode
        ) {
            return Insert((IEnumerable<T>) new T[] { document }, safeMode);
        }

        public MongoWriteResult Insert<T>(
            T[] documents,
            bool safeMode
        ) {
            return Insert((IEnumerable<T>) documents, safeMode);
        }

        public bool IsCapped() {
            throw new NotImplementedException();
        }

        // TODO: order of arguments is different in mongo shell
        public MongoMapReduceResult MapReduce(
            BsonDocument query,
            string map,
            string reduce,
            string outputCollection
        ) {
            throw new NotImplementedException();
        }

        public void ReIndex() {
            throw new NotImplementedException();
        }

        public MongoWriteResult Remove(
            BsonDocument query
        ) {
            return Remove(query, safeMode);
        }

        public MongoWriteResult Remove(
           BsonDocument query,
           bool safeMode
        ) {
            throw new NotImplementedException();
        }

        // TODO: what is dropTarget parameter in mongo shell?
        public void Rename(
            string newCollectionName
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult Save<T>(
            T document
        ) {
            return Save<T>(document, safeMode);
        }

        public MongoWriteResult Save<T>(
            T document,
            bool safeMode
        ) {
            throw new NotImplementedException();
        }

        public long StorageSize() {
            throw new NotImplementedException();
        }

        public long TotalIndexSize() {
            throw new NotImplementedException();
        }

        public long TotalSize() {
            throw new NotImplementedException();
        }

        public override string ToString() {
 	         return FullName;
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update
        ) {
            return Update<T>(query, update, false, false, safeMode);
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update,
            bool safeMode
        ) {
            return Update<T>(query, update, false, false, safeMode);
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update,
            bool upsert,
            bool multi
        ) {
            return Update<T>(query, update, upsert, multi, safeMode);
        }

        public MongoWriteResult Update<T>(
            BsonDocument query,
            T update,
            bool upsert,
            bool multi,
            bool safeMode
        ) {
            throw new NotImplementedException();
        }

        public MongoWriteResult UpdateMulti<T>(
            BsonDocument query,
            T update
        ) {
            return Update<T>(query, update, false, true, safeMode);
        }

        public MongoWriteResult UpdateMulti<T>(
            BsonDocument query,
            T update,
            bool safeMode
        ) {
            return Update<T>(query, update, false, true, safeMode);
        }

        public void Validate() {
            throw new NotImplementedException();
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
