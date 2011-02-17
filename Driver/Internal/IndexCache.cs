/* Copyright 2010-2011 10gen Inc.
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
using System.Threading;

namespace MongoDB.Driver.Internal {
    public class IndexCache {
        #region private fields
        private object syncRoot = new object();
        private HashSet<IndexCacheKey> cache = new HashSet<IndexCacheKey>();
        #endregion

        #region constructors
        public IndexCache() {
        }
        #endregion

        #region public methods
        public void Add(
            MongoCollection collection,
            string indexName
        ) {
            lock (syncRoot) {
                var database = collection.Database;
                var key = new IndexCacheKey(database.Name, collection.Name, indexName);
                cache.Add(key);
            }
        }

        public bool Contains(
            MongoCollection collection,
            string indexName
        ) {
            lock (syncRoot) {
                var database = collection.Database;
                var key = new IndexCacheKey(database.Name, collection.Name, indexName);
                return cache.Contains(key);
            }
        }

        public void Remove(
            MongoCollection collection,
            string indexName
        ) {
            lock (syncRoot) {
                var database = collection.Database;
                var key = new IndexCacheKey(database.Name, collection.Name, indexName);
                cache.Remove(key);
            }
        }

        public void Reset() {
            lock (syncRoot) {
                cache.Clear();
            }
        }

        public void Reset(
            MongoCollection collection
        ) {
            lock (syncRoot) {
                var database = collection.Database;
                cache.RemoveWhere(key => key.DatabaseName == database.Name && key.CollectionName == collection.Name);
            }
        }

        public void Reset(
            MongoDatabase database
        ) {
            lock (syncRoot) {
                cache.RemoveWhere(key => key.DatabaseName == database.Name);
            }
        }
        #endregion
    }

    internal struct IndexCacheKey {
        #region private fields
        private string databaseName;
        private string collectionName;
        private string indexName;
        private int hashCode; // can be calculated once because class is immutable
        #endregion

        #region constructors
        public IndexCacheKey(
            string databaseName,
            string collectionName,
            string indexName
        ) {
            this.databaseName = databaseName;
            this.collectionName = collectionName;
            this.indexName = indexName;
            this.hashCode = ComputeHashCode(databaseName, collectionName, indexName);
        }
        #endregion

        #region private static methods
        private static int ComputeHashCode(
            string databaseName,
            string collectionName,
            string indexName
        ) {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + databaseName.GetHashCode();
            hash = 37 * hash + collectionName.GetHashCode();
            hash = 37 * hash + indexName.GetHashCode();
            return hash;
        }
        #endregion

        #region public properties
        public string DatabaseName {
            get { return databaseName; }
        }

        public string CollectionName {
            get { return collectionName; }
        }

        public string IndexName {
            get { return indexName; }
        }

        public override bool Equals(object obj) {
            if (obj == null) { return false; }
            if (obj.GetType() != typeof(IndexCacheKey)) { return false; }
            var rhs = (IndexCacheKey) obj;
            return this.databaseName == rhs.databaseName && this.collectionName == rhs.collectionName && this.indexName == rhs.indexName;
        }

        public override int GetHashCode() {
            return hashCode;
        }

        public override string ToString() {
            return string.Format("{0}/{1}/{2}", databaseName, collectionName, indexName);
        }
        #endregion
    }
}
