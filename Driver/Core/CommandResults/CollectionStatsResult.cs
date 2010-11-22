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
using System.Text.RegularExpressions;

using MongoDB.Bson;

namespace MongoDB.Driver {
    [Serializable]
    public class CollectionStatsResult : CommandResult {
        #region private fields
        private IndexSizesResult indexSizes;
        #endregion

        #region constructors
        public CollectionStatsResult() {
        }
        #endregion

        #region public properties
        public double AverageObjectSize {
            get { return this["avgObjSize"].ToDouble(); }
        }

        public long DataSize {
            get { return this["size"].ToInt64(); }
        }

        public int ExtentCount {
            get { return this["numExtents"].AsInt32; }
        }

        public int Flags {
            get { return this["flags"].AsInt32; }
        }

        public int IndexCount {
            get { return this["nindexes"].AsInt32; }
        }

        public IndexSizesResult IndexSizes {
            get {
                if (indexSizes == null) {
                    // can't initialize indexSizes in the constructor because at that time the document is still empty
                    indexSizes = new IndexSizesResult(this["indexSizes"].AsBsonDocument);
                }
                return indexSizes;
            }
        }

        public long LastExtentSize {
            get { return this["lastExtentSize"].ToInt64(); }
        }

        public string Namespace {
            get { return this["ns"].AsString; }
        }

        public long ObjectCount {
            get { return this["count"].ToInt64(); }
        }

        public double PaddingFactor {
            get { return this["paddingFactor"].ToDouble(); }
        }

        public long StorageSize {
            get { return this["storageSize"].ToInt64(); }
        }

        public long TotalIndexSize {
            get { return this["totalIndexSize"].ToInt64(); }
        }
        #endregion

        #region nested classes
        public class IndexSizesResult {
            #region private fields
            private BsonDocument indexSizes;
            #endregion

            #region constructors
            public IndexSizesResult(
                BsonDocument indexSizes
            ) {
                this.indexSizes = indexSizes;
            }
            #endregion

            #region indexers
            public long this[
                string indexName
            ] {
                get { return indexSizes[indexName].ToInt64(); }
            }
            #endregion

            #region public properties
            public int Count {
                get { return indexSizes.ElementCount; }
            }

            public IEnumerable<string> Keys {
                get { return indexSizes.Names; }
            }

            public IEnumerable<long> Values {
                get { return indexSizes.Values.Select(v => v.ToInt64()); }
            }
            #endregion

            #region public methods
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
