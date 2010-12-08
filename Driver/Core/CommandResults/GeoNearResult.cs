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
using System.Text.RegularExpressions;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver {
    [Serializable]
    public class GeoNearResult<TDocument> : CommandResult {
        #region private fields
        private GeoNearHits hits;
        private GeoNearStats stats;
        #endregion

        #region constructors
        public GeoNearResult() {
        }
        #endregion

        #region public properties
        public GeoNearHits Hits {
            get {
                if (hits == null) {
                    hits = new GeoNearHits(response["results"].AsBsonArray);
                }
                return hits;
            }
        }

        public string Namespace {
            get { return response["ns"].AsString; }
        }

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
        public class GeoNearHits : IEnumerable<GeoNearHit> {
            #region private fields
            private List<GeoNearHit> hits;
            #endregion  

            #region constructors
            public GeoNearHits(
                BsonArray hits
            ) {
                this.hits = hits.Select(h => new GeoNearHit(h.AsBsonDocument)).ToList();
            }
            #endregion

            #region public properties
            public int Count {
                get { return hits.Count; }
            }
            #endregion

            #region indexers
            public GeoNearHit this[
                int index
            ] {
                get {
                    return hits[index];
                }
            }
            #endregion

            #region public methods
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

        public class GeoNearHit {
            #region private fields
            private BsonDocument hit;
            #endregion

            #region constructors
            public GeoNearHit(
                BsonDocument hit
            ) {
                this.hit = hit;
            }
            #endregion

            #region public properties
            public double Distance {
                get { return hit["dis"].ToDouble(); }
            }

            public TDocument Document {
                get {
                    if (typeof(TDocument) == typeof(BsonDocument)) {
                        return (TDocument) (object) RawDocument;
                    } else {
                        return BsonSerializer.Deserialize<TDocument>(RawDocument);
                    }
                }
            }

            public BsonDocument RawDocument {
                get { return hit["obj"].AsBsonDocument; }
            }
            #endregion
        }

        public class GeoNearStats {
            #region private fields
            private BsonDocument stats;
            #endregion

            #region constructors
            public GeoNearStats(
                BsonDocument stats
            ) {
                this.stats = stats;
            }
            #endregion

            #region public properties
            public double AverageDistance {
                get { return stats["avgDistance"].ToDouble(); }
            }

            public int BTreeLocations {
                get { return stats["btreelocs"].ToInt32(); }
            }

            public TimeSpan Duration {
                get { return TimeSpan.FromMilliseconds(stats["time"].ToInt32()); }
            }

            public double MaxDistance {
                get { return stats["maxDistance"].ToDouble(); }
            }

            public int NumberScanned {
                get { return stats["nscanned"].ToInt32(); }
            }

            public int ObjectsLoaded {
                get { return stats["objectsLoaded"].ToInt32(); }
            }
            #endregion
        }
        #endregion
    }
}
