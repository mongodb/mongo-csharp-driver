﻿/* Copyright 2010-2011 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents the result of a map/reduce command.
    /// </summary>
    public class MapReduceResult : CommandResult {
        #region private fields
        private MongoDatabase inputDatabase;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the MapReduceResult class.
        /// </summary>
        public MapReduceResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the output collection name (null if none).
        /// </summary>
        public string CollectionName {
            get {
                var result = response["result", null];
                if (result != null) {
                    if (result.IsString) {
                        return result.AsString;
                    } else {
                        return (string) result.AsBsonDocument["collection", null];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the output database name (null if none).
        /// </summary>
        public string DatabaseName {
            get {
                var result = response["result", null];
                if (result != null && result.IsBsonDocument) {
                    return (string) result.AsBsonDocument["db", null];
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the duration.
        /// </summary>
        public TimeSpan Duration {
            get { return TimeSpan.FromMilliseconds(response["timeMillis"].ToInt32()); }
        }

        /// <summary>
        /// Gets the emit count.
        /// </summary>
        public int EmitCount {
            get { return response["counts"].AsBsonDocument["emit"].ToInt32(); }
        }

        /// <summary>
        /// Gets the output count.
        /// </summary>
        public int OutputCount {
            get { return response["counts"].AsBsonDocument["output"].ToInt32(); }
        }

        /// <summary>
        /// Gets the inline results.
        /// </summary>
        public IEnumerable<BsonDocument> InlineResults {
            get { return response["results"].AsBsonArray.Cast<BsonDocument>(); }
        }

        /// <summary>
        /// Gets the input count.
        /// </summary>
        public int InputCount {
            get { return response["counts"].AsBsonDocument["input"].ToInt32(); }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Gets the inline results as TDocuments.
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents.</typeparam>
        /// <returns>The documents.</returns>
        public IEnumerable<TDocument> GetInlineResultsAs<TDocument>() {
            return InlineResults.Select(document => BsonSerializer.Deserialize<TDocument>(document));
        }

        /// <summary>
        /// Gets the results (either inline or fetched from the output collection).
        /// </summary>
        /// <returns>The documents.</returns>
        public IEnumerable<BsonDocument> GetResults() {
            if (response.Contains("results")) {
                return InlineResults;
            } else {
                var outputDatabaseName = DatabaseName;
                MongoDatabase outputDatabase;
                if (outputDatabaseName == null) {
                    outputDatabase = inputDatabase;
                } else {
                    outputDatabase = inputDatabase.Server[outputDatabaseName];
                }
                return outputDatabase[CollectionName].FindAll();
            }
        }

        /// <summary>
        /// Gets the results as TDocuments (either inline or fetched from the output collection).
        /// </summary>
        /// <typeparam name="TDocument">The type of the documents.</typeparam>
        /// <returns>The documents.</returns>
        public IEnumerable<TDocument> GetResultsAs<TDocument>() {
            if (response.Contains("results")) {
                return GetInlineResultsAs<TDocument>();
            } else {
                var outputDatabaseName = DatabaseName;
                MongoDatabase outputDatabase;
                if (outputDatabaseName == null) {
                    outputDatabase = inputDatabase;
                } else {
                    outputDatabase = inputDatabase.Server[outputDatabaseName];
                }
                return outputDatabase[CollectionName].FindAllAs<TDocument>();
            }
        }
        #endregion

        #region internal methods
        internal void SetInputDatabase(
            MongoDatabase inputDatabase
        ) {
            this.inputDatabase = inputDatabase;
        }
        #endregion
    }
}
