/* Copyright 2010-2013 10gen Inc.
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

using System.Collections.Generic;

namespace MongoDB.Driver.Internal
{
    /// <summary>
    /// Represents a cache of the names of indexes that are known to exist on a given server.
    /// </summary>
    public class IndexCache
    {
        // private fields
        private object _syncRoot = new object();
        private HashSet<IndexCacheKey> _cache = new HashSet<IndexCacheKey>();

        // constructors
        /// <summary>
        /// Initializes a new instance of the IndexCache class.
        /// </summary>
        public IndexCache()
        {
        }

        // public methods
        /// <summary>
        /// Adds the name of an index to the cache.
        /// </summary>
        /// <param name="collection">The collection that contains the index.</param>
        /// <param name="indexName">The name of the index.</param>
        public void Add(MongoCollection collection, string indexName)
        {
            lock (_syncRoot)
            {
                var database = collection.Database;
                var key = new IndexCacheKey(database.Name, collection.Name, indexName);
                _cache.Add(key);
            }
        }

        /// <summary>
        /// Tests whether the cache contains the name of an index.
        /// </summary>
        /// <param name="collection">The collection that contains the index.</param>
        /// <param name="indexName">The name of the index.</param>
        /// <returns>True if the cache contains the named index.</returns>
        public bool Contains(MongoCollection collection, string indexName)
        {
            lock (_syncRoot)
            {
                var database = collection.Database;
                var key = new IndexCacheKey(database.Name, collection.Name, indexName);
                return _cache.Contains(key);
            }
        }

        /// <summary>
        /// Removes the name of an index from the cache.
        /// </summary>
        /// <param name="collection">The collection that contains the index.</param>
        /// <param name="indexName">The name of the index.</param>
        public void Remove(MongoCollection collection, string indexName)
        {
            lock (_syncRoot)
            {
                var database = collection.Database;
                var key = new IndexCacheKey(database.Name, collection.Name, indexName);
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// Resets the cache.
        /// </summary>
        public void Reset()
        {
            lock (_syncRoot)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Resets part of the cache by removing all indexes for a collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public void Reset(MongoCollection collection)
        {
            Reset(collection.Database.Name, collection.Name);
        }

        /// <summary>
        /// Resets part of the cache by removing all indexes for a database.
        /// </summary>
        /// <param name="database">The database.</param>
        public void Reset(MongoDatabase database)
        {
            Reset(database.Name);
        }

        /// <summary>
        /// Resets part of the cache by removing all indexes for a database.
        /// </summary>
        /// <param name="databaseName">The name of the database.</param>
        public void Reset(string databaseName)
        {
            lock (_syncRoot)
            {
                _cache.RemoveWhere(key => key.DatabaseName == databaseName);
            }
        }

        /// <summary>
        /// Resets part of the cache by removing all indexes for a collection.
        /// </summary>
        /// <param name="databaseName">The name of the database containing the collection.</param>
        /// <param name="collectionName">The name of the collection.</param>
        public void Reset(string databaseName, string collectionName)
        {
            lock (_syncRoot)
            {
                _cache.RemoveWhere(key => key.DatabaseName == databaseName && key.CollectionName == collectionName);
            }
        }
    }

    internal struct IndexCacheKey
    {
        // private fields
        private string _databaseName;
        private string _collectionName;
        private string _indexName;
        private int _hashCode; // can be calculated once because class is immutable

        // constructors
        public IndexCacheKey(string databaseName, string collectionName, string indexName)
        {
            _databaseName = databaseName;
            _collectionName = collectionName;
            _indexName = indexName;
            _hashCode = ComputeHashCode(databaseName, collectionName, indexName);
        }

        // private static methods
        private static int ComputeHashCode(string databaseName, string collectionName, string indexName)
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + databaseName.GetHashCode();
            hash = 37 * hash + collectionName.GetHashCode();
            hash = 37 * hash + indexName.GetHashCode();
            return hash;
        }

        // public properties
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string IndexName
        {
            get { return _indexName; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            if (obj.GetType() != typeof(IndexCacheKey)) { return false; }
            var rhs = (IndexCacheKey)obj;
            return _databaseName == rhs._databaseName && _collectionName == rhs._collectionName && _indexName == rhs._indexName;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}", _databaseName, _collectionName, _indexName);
        }
    }
}
