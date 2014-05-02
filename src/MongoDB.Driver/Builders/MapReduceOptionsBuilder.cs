/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Builders
{
    /// <summary>
    /// Represents the output options of a map/reduce operation.
    /// </summary>
    [Obsolete("Use MapReduceArgs instead.")]
    public class MapReduceOutput
    {
        // private fields
        private MapReduceOutputMode _mode;
        private string _databaseName;
        private string _collectionName;
        private bool _sharded;

        // constructors
        /// <summary>
        /// Creates a new instance of the MapReduceOutput class.
        /// </summary>
        public MapReduceOutput()
        {
            _mode = MapReduceOutputMode.Inline;
        }

        /// <summary>
        /// Creates a new instance of the MapReduceOutput class.
        /// </summary>
        /// <param name="collectionName">The name of the output collection.</param>
        public MapReduceOutput(string collectionName)
        {
            _mode = MapReduceOutputMode.Replace;
            _collectionName = collectionName;
        }

        /// <summary>
        /// Creates a new instance of the MapReduceOutput class.
        /// </summary>
        /// <param name="databaseName">The name of the database that will contain the output collection.</param>
        /// <param name="collectionName">The name of the output collection.</param>
        public MapReduceOutput(string databaseName, string collectionName)
        {
            _mode = MapReduceOutputMode.Replace;
            _databaseName = databaseName;
            _collectionName = collectionName;
        }

        // implicit operators
        /// <summary>
        /// Allows strings to be implicitly used as the name of the output collection.
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static implicit operator MapReduceOutput(string collectionName)
        {
            return new MapReduceOutput(collectionName);
        }

        // public static properties
        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should returned inline.
        /// </summary>
        public static MapReduceOutput Inline
        {
            get
            {
                return new MapReduceOutput();
            }
        }

        // public properties
        /// <summary>
        /// Gets or sets the name of the output collection.
        /// </summary>
        public string CollectionName
        {
            get { return _collectionName; }
            set
            {
                _collectionName = value;
                if (_collectionName != null && _mode == MapReduceOutputMode.Inline)
                {
                    _mode = MapReduceOutputMode.Replace;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the database that will contain the output collection.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
            set
            {
                _databaseName = value;
                if (_databaseName != null && _mode == MapReduceOutputMode.Inline)
                {
                    _mode = MapReduceOutputMode.Replace;
                }
            }
        }

        /// <summary>
        /// Gets or sets the output mode for the results of the map reduce operation.
        /// </summary>
        public MapReduceOutputMode Mode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                if (_mode == MapReduceOutputMode.Inline)
                {
                    _databaseName = null;
                    _collectionName = null;
                    _sharded = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the output collection is sharded.
        /// </summary>
        public bool Sharded
        {
            get { return _sharded; }
            set { _sharded = value; }
        }

        // public static methods
        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (replaces the entire collection).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Replace(string collectionName)
        {
            return new MapReduceOutput(collectionName);
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (replaces the entire collection).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <param name="sharded">Whether the output collection is sharded.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Replace(string collectionName, bool sharded)
        {
            return new MapReduceOutput(collectionName) { Sharded = sharded };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (replaces the entire collection).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Replace(string databaseName, string collectionName)
        {
            return new MapReduceOutput(databaseName, collectionName);
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (replaces the entire collection).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <param name="sharded">Whether the output collection is sharded.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Replace(string databaseName, string collectionName, bool sharded)
        {
            return new MapReduceOutput(databaseName, collectionName) { Sharded = sharded };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (adding new values and overwriting existing ones).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Merge(string collectionName)
        {
            return new MapReduceOutput(collectionName) { Mode = MapReduceOutputMode.Merge };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (adding new values and overwriting existing ones).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <param name="sharded">Whether the output collection is sharded.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Merge(string collectionName, bool sharded)
        {
            return new MapReduceOutput(collectionName) { Mode = MapReduceOutputMode.Merge, Sharded = sharded };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (adding new values and overwriting existing ones).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Merge(string databaseName, string collectionName)
        {
            return new MapReduceOutput(databaseName, collectionName) { Mode = MapReduceOutputMode.Merge };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (adding new values and overwriting existing ones).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <param name="sharded">Whether the output collection is sharded.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Merge(string databaseName, string collectionName, bool sharded)
        {
            return new MapReduceOutput(databaseName, collectionName) { Mode = MapReduceOutputMode.Merge, Sharded = sharded };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (using the reduce function to combine new values with existing values).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Reduce(string collectionName)
        {
            return new MapReduceOutput(collectionName) { Mode = MapReduceOutputMode.Reduce };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (using the reduce function to combine new values with existing values).
        /// </summary>
        /// <param name="collectionName">The output collection name.</param>
        /// <param name="sharded">Whether the output collection is sharded.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Reduce(string collectionName, bool sharded)
        {
            return new MapReduceOutput(collectionName) { Mode = MapReduceOutputMode.Reduce, Sharded = sharded };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (using the reduce function to combine new values with existing values).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Reduce(string databaseName, string collectionName)
        {
            return new MapReduceOutput(databaseName, collectionName) { Mode = MapReduceOutputMode.Reduce };
        }

        /// <summary>
        /// Gets a MapReduceOutput value that specifies that the output should be stored in a collection (using the reduce function to combine new values with existing values).
        /// </summary>
        /// <param name="databaseName">The output database name.</param>
        /// <param name="collectionName">The output collection name.</param>
        /// <param name="sharded">Whether the output collection is sharded.</param>
        /// <returns>A MapReduceOutput.</returns>
        public static MapReduceOutput Reduce(string databaseName, string collectionName, bool sharded)
        {
            return new MapReduceOutput(databaseName, collectionName) { Mode = MapReduceOutputMode.Reduce, Sharded = sharded };
        }

        // internal methods
        internal BsonValue ToBsonValue()
        {
            if (_mode == MapReduceOutputMode.Inline)
            {
                if (_sharded)
                {
                    throw new MongoException("MapReduceOutput cannot be sharded when output mode is Inline.");
                }
                return new BsonDocument("inline", 1);
            }
            else
            {
                if (_collectionName == null)
                {
                    throw new MongoException("MapReduceOutput collection name is missing.");
                }
                if (_mode == MapReduceOutputMode.Replace && _databaseName == null && !_sharded)
                {
                    return _collectionName;
                }
                else
                {
                    string modeString = "replace";
                    switch (_mode)
                    {
                        case MapReduceOutputMode.Merge: modeString = "merge"; break;
                        case MapReduceOutputMode.Reduce: modeString = "reduce"; break;
                    }
                    return new BsonDocument
                    {
                        { modeString, _collectionName },
                        { "db", _databaseName, _databaseName != null }, // optional
                        { "sharded", true, _sharded } // optional
                    };
                }
            }
        }
    }

    /// <summary>
    /// A builder for the options of a Map/Reduce operation.
    /// </summary>
    [Obsolete("Use MapReduceArgs instead.")]
    public static class MapReduceOptions
    {
        // public static properties
        /// <summary>
        /// Gets a null value with a type of IMongoMapReduceOptions.
        /// </summary>
        public static IMongoMapReduceOptions Null
        {
            get { return null; }
        }

        // public static methods
        /// <summary>
        /// Sets the finalize function.
        /// </summary>
        /// <param name="finalize">The finalize function.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetFinalize(BsonJavaScript finalize)
        {
            return new MapReduceOptionsBuilder().SetFinalize(finalize);
        }

        /// <summary>
        /// Sets whether to use jsMode for the map reduce operation.
        /// </summary>
        /// <param name="value">Whether to use jsMode.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetJSMode(bool value)
        {
            return new MapReduceOptionsBuilder().SetJSMode(value);
        }

        /// <summary>
        /// Sets whether to keep the temp collection (obsolete in 1.8.0+).
        /// </summary>
        /// <param name="value">Whether to keep the temp collection.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetKeepTemp(bool value)
        {
            return new MapReduceOptionsBuilder().SetKeepTemp(value);
        }

        /// <summary>
        /// Sets the number of documents to send to the map function (useful in combination with SetSortOrder).
        /// </summary>
        /// <param name="value">The number of documents to send to the map function.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetLimit(int value)
        {
            return new MapReduceOptionsBuilder().SetLimit(value);
        }

        /// <summary>
        /// Sets the output option (see MapReduceOutput).
        /// </summary>
        /// <param name="output">The output option.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetOutput(MapReduceOutput output)
        {
            return new MapReduceOptionsBuilder().SetOutput(output);
        }

        /// <summary>
        /// Sets the optional query that filters which documents are sent to the map function (also useful in combination with SetSortOrder and SetLimit).
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetQuery(IMongoQuery query)
        {
            return new MapReduceOptionsBuilder().SetQuery(query);
        }

        /// <summary>
        /// Sets a scope that contains variables that can be accessed by the map, reduce and finalize functions.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetScope(IMongoScope scope)
        {
            return new MapReduceOptionsBuilder().SetScope(scope);
        }

        /// <summary>
        /// Sets the sort order (useful in combination with SetLimit, your map function should not depend on the order the documents are sent to it).
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetSortOrder(IMongoSortBy sortBy)
        {
            return new MapReduceOptionsBuilder().SetSortOrder(sortBy);
        }

        /// <summary>
        /// Sets the sort order (useful in combination with SetLimit, your map function should not depend on the order the documents are sent to it).
        /// </summary>
        /// <param name="keys">The names of the keys to sort by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetSortOrder(params string[] keys)
        {
            return new MapReduceOptionsBuilder().SetSortOrder(SortBy.Ascending(keys));
        }

        /// <summary>
        /// Sets whether the server should be more verbose when logging map/reduce operations.
        /// </summary>
        /// <param name="value">Whether the server should be more verbose.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public static MapReduceOptionsBuilder SetVerbose(bool value)
        {
            return new MapReduceOptionsBuilder().SetVerbose(value);
        }
    }

    /// <summary>
    /// A builder for the options of a Map/Reduce operation.
    /// </summary>
    [Serializable]
    [Obsolete("Use MapReduceArgs instead.")]
    [BsonSerializer(typeof(MapReduceOptionsBuilder.Serializer))]
    public class MapReduceOptionsBuilder : BuilderBase, IMongoMapReduceOptions
    {
        // private fields
        private BsonDocument _document;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MapReduceOptionsBuilder class.
        /// </summary>
        public MapReduceOptionsBuilder()
        {
            _document = new BsonDocument();
        }

        // public methods
        /// <summary>
        /// Sets the finalize function.
        /// </summary>
        /// <param name="finalize">The finalize function.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetFinalize(BsonJavaScript finalize)
        {
            _document["finalize"] = finalize;
            return this;
        }

        /// <summary>
        /// Sets whether to use jsMode for the map reduce operation.
        /// </summary>
        /// <param name="value">Whether to use jsMode.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetJSMode(bool value)
        {
            _document["jsMode"] = value;
            return this;
        }

        /// <summary>
        /// Sets whether to keep the temp collection (obsolete in 1.8.0+).
        /// </summary>
        /// <param name="value">Whether to keep the temp collection.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetKeepTemp(bool value)
        {
            _document["keeptemp"] = value;
            return this;
        }

        /// <summary>
        /// Sets the number of documents to send to the map function (useful in combination with SetSortOrder).
        /// </summary>
        /// <param name="value">The number of documents to send to the map function.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetLimit(int value)
        {
            _document["limit"] = value;
            return this;
        }

        /// <summary>
        /// Sets the output option (see MapReduceOutput).
        /// </summary>
        /// <param name="output">The output option.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetOutput(MapReduceOutput output)
        {
            _document["out"] = output.ToBsonValue();
            return this;
        }

        /// <summary>
        /// Sets the optional query that filters which documents are sent to the map function (also useful in combination with SetSortOrder and SetLimit).
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetQuery(IMongoQuery query)
        {
            _document["query"] = BsonDocumentWrapper.Create(query);
            return this;
        }

        /// <summary>
        /// Sets a scope that contains variables that can be accessed by the map, reduce and finalize functions.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetScope(IMongoScope scope)
        {
            _document["scope"] = BsonDocumentWrapper.Create(scope);
            return this;
        }

        /// <summary>
        /// Sets the sort order (useful in combination with SetLimit, your map function should not depend on the order the documents are sent to it).
        /// </summary>
        /// <param name="sortBy">The sort order.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetSortOrder(IMongoSortBy sortBy)
        {
            _document["sort"] = BsonDocumentWrapper.Create(sortBy);
            return this;
        }

        /// <summary>
        /// Sets the sort order (useful in combination with SetLimit, your map function should not depend on the order the documents are sent to it).
        /// </summary>
        /// <param name="keys">The names of the keys to sort by.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetSortOrder(params string[] keys)
        {
            return SetSortOrder(SortBy.Ascending(keys));
        }

        /// <summary>
        /// Sets whether the server should be more verbose when logging map/reduce operations.
        /// </summary>
        /// <param name="value">Whether the server should be more verbose.</param>
        /// <returns>The builder (so method calls can be chained).</returns>
        public MapReduceOptionsBuilder SetVerbose(bool value)
        {
            _document["verbose"] = value;
            return this;
        }

        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public override BsonDocument ToBsonDocument()
        {
            return _document;
        }

        // internal methods
        internal MapReduceOptionsBuilder AddOptions(BsonDocument options)
        {
            _document.AddRange(options);
            return this;
        }

        // nested classes
        new internal class Serializer : SerializerBase<MapReduceOptionsBuilder>
        {
            public override void Serialize(BsonSerializationContext context, MapReduceOptionsBuilder value)
            {
                context.SerializeWithChildContext(BsonDocumentSerializer.Instance, value._document);
            }
        }
    }
}
