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
using System.Text.RegularExpressions;

using MongoDB.Bson;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents the results of the collection stats command.
    /// </summary>
    [Serializable]
    public class CollectionStatsResult : CommandResult {
        #region private fields
        private IndexSizesResult indexSizes;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the CollectionStatsResult class.
        /// </summary>
        public CollectionStatsResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the average object size.
        /// </summary>
        public double AverageObjectSize {
            get { return response["avgObjSize"].ToDouble(); }
        }

        /// <summary>
        /// Gets the data size.
        /// </summary>
        public long DataSize {
            get { return response["size"].ToInt64(); }
        }

        /// <summary>
        /// Gets the extent count.
        /// </summary>
        public int ExtentCount {
            get { return response["numExtents"].AsInt32; }
        }

        /// <summary>
        /// Gets the flags.
        /// </summary>
        public int Flags {
            get { return response["flags"].AsInt32; }
        }

        /// <summary>
        /// Gets the index count.
        /// </summary>
        public int IndexCount {
            get { return response["nindexes"].AsInt32; }
        }

        /// <summary>
        /// Gets the index sizes.
        /// </summary>
        public IndexSizesResult IndexSizes {
            get {
                if (indexSizes == null) {
                    // can't initialize indexSizes in the constructor because at that time the document is still empty
                    indexSizes = new IndexSizesResult(response["indexSizes"].AsBsonDocument);
                }
                return indexSizes;
            }
        }

        /// <summary>
        /// Gets the last extent size.
        /// </summary>
        public long LastExtentSize {
            get { return response["lastExtentSize"].ToInt64(); }
        }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public string Namespace {
            get { return response["ns"].AsString; }
        }

        /// <summary>
        /// Gets the object count.
        /// </summary>
        public long ObjectCount {
            get { return response["count"].ToInt64(); }
        }

        /// <summary>
        /// Gets the padding factor.
        /// </summary>
        public double PaddingFactor {
            get { return response["paddingFactor"].ToDouble(); }
        }

        /// <summary>
        /// Gets the storage size.
        /// </summary>
        public long StorageSize {
            get { return response["storageSize"].ToInt64(); }
        }

        /// <summary>
        /// Gets the total index size.
        /// </summary>
        public long TotalIndexSize {
            get { return response["totalIndexSize"].ToInt64(); }
        }
        #endregion

        #region nested classes
        /// <summary>
        /// Represents a collection of index sizes.
        /// </summary>
        public class IndexSizesResult {
            #region private fields
            private BsonDocument indexSizes;
            #endregion

            #region constructors
            /// <summary>
            /// Initializes a new instance of the IndexSizesResult class.
            /// </summary>
            /// <param name="indexSizes">The index sizes document.</param>
            public IndexSizesResult(
                BsonDocument indexSizes
            ) {
                this.indexSizes = indexSizes;
            }
            #endregion

            #region indexers
            /// <summary>
            /// Gets the size of an index.
            /// </summary>
            /// <param name="indexName">The name of the index.</param>
            /// <returns>The size of the index.</returns>
            public long this[
                string indexName
            ] {
                get { return indexSizes[indexName].ToInt64(); }
            }
            #endregion

            #region public properties
            /// <summary>
            /// Gets the count of indexes.
            /// </summary>
            public int Count {
                get { return indexSizes.ElementCount; }
            }

            /// <summary>
            /// Gets the names of the indexes.
            /// </summary>
            public IEnumerable<string> Keys {
                get { return indexSizes.Names; }
            }

            /// <summary>
            /// Gets the sizes of the indexes.
            /// </summary>
            public IEnumerable<long> Values {
                get { return indexSizes.Values.Select(v => v.ToInt64()); }
            }
            #endregion

            #region public methods
            /// <summary>
            /// Tests whether the results contain the size of an index.
            /// </summary>
            /// <param name="indexName">The name of the index.</param>
            /// <returns>True if the results contain the size of the index.</returns>
            public bool ContainsKey(
                string indexName
            ) {
                return indexSizes.Contains(indexName);
            }
            #endregion
        }
        #endregion
    }
}
