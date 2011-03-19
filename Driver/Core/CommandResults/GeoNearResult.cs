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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver {
    /// <summary>
    /// Represents the result of a GeoNear command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the returned documents.</typeparam>
    [Serializable]
    public class GeoNearResult<TDocument> : CommandResult {
        #region private fields
        private GeoNearHits hits;
        private GeoNearStats stats;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the GeoNearResult class.
        /// </summary>
        public GeoNearResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        public GeoNearHits Hits {
            get {
                if (hits == null) {
                    hits = new GeoNearHits(response["results"].AsBsonArray);
                }
                return hits;
            }
        }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public string Namespace {
            get { return response["ns"].AsString; }
        }

        /// <summary>
        /// Gets the stats.
        /// </summary>
        public GeoNearStats Stats {
            get {
                if (stats == null) {
                    stats = new GeoNearStats(response["stats"].AsBsonDocument);
                }
                return stats;
            }
        }
        #endregion

        #region nested classes
        /// <summary>
        /// Represents a collection of GeoNear hits.
        /// </summary>
        public class GeoNearHits : IEnumerable<GeoNearHit> {
            #region private fields
            private List<GeoNearHit> hits;
            #endregion  

            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHits command.
            /// </summary>
            /// <param name="hits">The hits.</param>
            public GeoNearHits(
                BsonArray hits
            ) {
                this.hits = hits.Select(h => new GeoNearHit(h.AsBsonDocument)).ToList();
            }
            #endregion

            #region public properties
            /// <summary>
            /// Gets the count of the number of hits.
            /// </summary>
            public int Count {
                get { return hits.Count; }
            }
            #endregion

            #region indexers
            /// <summary>
            /// Gets an individual hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            public GeoNearHit this[
                int index
            ] {
                get {
                    return hits[index];
                }
            }
            #endregion

            #region public methods
            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            public IEnumerator<GeoNearHit> GetEnumerator() {
                return hits.GetEnumerator();
            }
            #endregion

            #region explicit interface implementations
            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
            #endregion
        }

        /// <summary>
        /// Represents a GeoNear hit.
        /// </summary>
        public class GeoNearHit {
            #region private fields
            private BsonDocument hit;
            #endregion

            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHit class.
            /// </summary>
            /// <param name="hit">The hit.</param>
            public GeoNearHit(
                BsonDocument hit
            ) {
                this.hit = hit;
            }
            #endregion

            #region public properties
            /// <summary>
            /// Gets the distance.
            /// </summary>
            public double Distance {
                get { return hit["dis"].ToDouble(); }
            }

            /// <summary>
            /// Gets the document.
            /// </summary>
            public TDocument Document {
                get {
                    if (typeof(TDocument) == typeof(BsonDocument)) {
                        return (TDocument) (object) RawDocument;
                    } else {
                        return BsonSerializer.Deserialize<TDocument>(RawDocument);
                    }
                }
            }

            /// <summary>
            /// Gets the document as a BsonDocument.
            /// </summary>
            public BsonDocument RawDocument {
                get { return hit["obj"].AsBsonDocument; }
            }
            #endregion
        }

        /// <summary>
        /// Represents the stats of a GeoNear command.
        /// </summary>
        public class GeoNearStats {
            #region private fields
            private BsonDocument stats;
            #endregion

            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearStats class.
            /// </summary>
            /// <param name="stats">The stats.</param>
            public GeoNearStats(
                BsonDocument stats
            ) {
                this.stats = stats;
            }
            #endregion

            #region public properties
            /// <summary>
            /// Gets the average distance.
            /// </summary>
            public double AverageDistance {
                get { return stats["avgDistance"].ToDouble(); }
            }

            /// <summary>
            /// Gets the count of b-tree locations.
            /// </summary>
            public int BTreeLocations {
                get { return stats["btreelocs"].ToInt32(); }
            }

            /// <summary>
            /// Gets the duration.
            /// </summary>
            public TimeSpan Duration {
                get { return TimeSpan.FromMilliseconds(stats["time"].ToInt32()); }
            }

            /// <summary>
            /// Gets the max distance.
            /// </summary>
            public double MaxDistance {
                get { return stats["maxDistance"].ToDouble(); }
            }

            /// <summary>
            /// Gets the number of documents scanned.
            /// </summary>
            public int NumberScanned {
                get { return stats["nscanned"].ToInt32(); }
            }

            /// <summary>
            /// Gets the number of documents loaded.
            /// </summary>
            public int ObjectsLoaded {
                get { return stats["objectsLoaded"].ToInt32(); }
            }
            #endregion
        }
        #endregion
    }
}
