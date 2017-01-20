/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents collection system flags.
    /// </summary>
    [Flags]
    public enum CollectionSystemFlags
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// The collection has an _id index.
        /// </summary>
        HasIdIndex = 1 // called HaveIdIndex in the server but renamed here to follow .NET naming conventions
    }

    /// <summary>
    /// Represents collection user flags.
    /// </summary>
    [Flags]
    public enum CollectionUserFlags
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// User power of 2 size.
        /// </summary>
        UsePowerOf2Sizes = 1,
        /// <summary>
        /// Whether padding should not be used.
        /// </summary>
        NoPadding = 2
    }

    /// <summary>
    /// Represents the results of the collection stats command.
    /// </summary>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(CommandResultSerializer<CollectionStatsResult>))]
    public class CollectionStatsResult : CommandResult
    {
        // private fields
        private IndexSizesResult _indexSizes;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionStatsResult"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public CollectionStatsResult(BsonDocument response)
            : base(response)
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
            get { return Response["numExtents"].ToInt32(); }
        }

        /// <summary>
        /// Gets the index count.
        /// </summary>
        public int IndexCount
        {
            get { return Response["nindexes"].ToInt32(); }
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
        /// Gets a value indicating whether the collection is capped.
        /// </summary>
        public bool IsCapped
        {
            get { return Response.GetValue("capped", false).ToBoolean(); }
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
            get { return Response.GetValue("max", 0).ToInt32(); }
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
        /// Gets the system flags.
        /// </summary>
        public CollectionSystemFlags SystemFlags
        {
            get
            {
                // systemFlags was first introduced in server version 2.2 (check "flags" also for compatibility with older servers)
                BsonValue systemFlags;
                if (Response.TryGetValue("systemFlags", out systemFlags) || Response.TryGetValue("flags", out systemFlags))
                {
                    return (CollectionSystemFlags)systemFlags.ToInt32();
                }
                else
                {
                    return CollectionSystemFlags.HasIdIndex;
                }
            }
        }

        /// <summary>
        /// Gets the total index size.
        /// </summary>
        public long TotalIndexSize
        {
            get { return Response["totalIndexSize"].ToInt64(); }
        }

        /// <summary>
        /// Gets the user flags.
        /// </summary>
        public CollectionUserFlags UserFlags
        {
            get
            {
                // userFlags was first introduced in server version 2.2
                BsonValue userFlags;
                if (Response.TryGetValue("userFlags", out userFlags))
                {
                    return (CollectionUserFlags)userFlags.ToInt32();
                }
                else
                {
                    return CollectionUserFlags.None;
                }
            }
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
