/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the results of the collection stats command.
    /// </summary>
    [Serializable]
    public class CollectionStatsResult : CommandResult
    {
        // private fields
        private IndexSizesResult _indexSizes;

        // constructors
        /// <summary>
        /// Initializes a new instance of the CollectionStatsResult class.
        /// </summary>
        public CollectionStatsResult()
        {
        }

        // public properties
        /// <summary>
        /// Gets the average object size.
        /// </summary>
        public double AverageObjectSize
        {
            get { return Response["avgObjSize"].ToDouble(); }
        }

        /// <summary>
        /// Gets the data size.
        /// </summary>
        public long DataSize
        {
            get { return Response["size"].ToInt64(); }
        }

        /// <summary>
        /// Gets the extent count.
        /// </summary>
        public int ExtentCount
        {
            get { return Response["numExtents"].AsInt32; }
        }

        /// <summary>
        /// Gets the flags.
        /// </summary>
        public int Flags
        {
            get { return Response["flags"].AsInt32; }
        }

        /// <summary>
        /// Gets the index count.
        /// </summary>
        public int IndexCount
        {
            get { return Response["nindexes"].AsInt32; }
        }

        /// <summary>
        /// Gets the index sizes.
        /// </summary>
        public IndexSizesResult IndexSizes
        {
            get
            {
                if (_indexSizes == null)
                {
                    // can't initialize indexSizes in the constructor because at that time the document is still empty
                    _indexSizes = new IndexSizesResult(Response["indexSizes"].AsBsonDocument);
                }
                return _indexSizes;
            }
        }

        /// <summary>
        /// Gets whether the collection is capped.
        /// </summary>
        public bool IsCapped
        {
            get { return Response["capped", false].ToBoolean(); }
        }

        /// <summary>
        /// Gets the last extent size.
        /// </summary>
        public long LastExtentSize
        {
            get { return Response["lastExtentSize"].ToInt64(); }
        }

        /// <summary>
        /// Gets the index count.
        /// </summary>
        public long MaxDocuments
        {
            get { return Response["max", 0].AsInt32; }
        }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public string Namespace
        {
            get { return Response["ns"].AsString; }
        }

        /// <summary>
        /// Gets the object count.
        /// </summary>
        public long ObjectCount
        {
            get { return Response["count"].ToInt64(); }
        }

        /// <summary>
        /// Gets the padding factor.
        /// </summary>
        public double PaddingFactor
        {
            get { return Response["paddingFactor"].ToDouble(); }
        }

        /// <summary>
        /// Gets the storage size.
        /// </summary>
        public long StorageSize
        {
            get { return Response["storageSize"].ToInt64(); }
        }

        /// <summary>
        /// Gets the total index size.
        /// </summary>
        public long TotalIndexSize
        {
            get { return Response["totalIndexSize"].ToInt64(); }
        }

        // nested classes
        /// <summary>
        /// Represents a collection of index sizes.
        /// </summary>
        public class IndexSizesResult
        {
            // private fields
            private BsonDocument _indexSizes;

            // constructors
            /// <summary>
            /// Initializes a new instance of the IndexSizesResult class.
            /// </summary>
            /// <param name="indexSizes">The index sizes document.</param>
            public IndexSizesResult(BsonDocument indexSizes)
            {
                _indexSizes = indexSizes;
            }

            // indexers
            /// <summary>
            /// Gets the size of an index.
            /// </summary>
            /// <param name="indexName">The name of the index.</param>
            /// <returns>The size of the index.</returns>
            public long this[string indexName]
            {
                get { return _indexSizes[indexName].ToInt64(); }
            }

            // public properties
            /// <summary>
            /// Gets the count of indexes.
            /// </summary>
            public int Count
            {
                get { return _indexSizes.ElementCount; }
            }

            /// <summary>
            /// Gets the names of the indexes.
            /// </summary>
            public IEnumerable<string> Keys
            {
                get { return _indexSizes.Names; }
            }

            /// <summary>
            /// Gets the sizes of the indexes.
            /// </summary>
            public IEnumerable<long> Values
            {
                get { return _indexSizes.Values.Select(v => v.ToInt64()); }
            }

            // public methods
            /// <summary>
            /// Tests whether the results contain the size of an index.
            /// </summary>
            /// <param name="indexName">The name of the index.</param>
            /// <returns>True if the results contain the size of the index.</returns>
            public bool ContainsKey(string indexName)
            {
                return _indexSizes.Contains(indexName);
            }
        }
    }
}
