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
    [Serializable]
    public abstract class GeoNearResult: CommandResult {
        #region private fields
        private GeoNearStats stats;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the GeoNearResult class.
        /// </summary>
        protected GeoNearResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        public GeoNearHits Hits {
            get { return HitsImplementation; }
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

        #region protected properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        protected abstract GeoNearHits HitsImplementation { get; }
        #endregion

        #region nested classes
        /// <summary>
        /// Represents a collection of GeoNear hits.
        /// </summary>
        public abstract class GeoNearHits : IEnumerable {
            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHits class.
            /// </summary>
            protected GeoNearHits() {
            }
            #endregion

            #region public properties
            /// <summary>
            /// Gets the count of the number of hits.
            /// </summary>
            public abstract int Count { get; }
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
                    return GetHitImplementation(index);
                }
            }
            #endregion

            #region public methods
            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            public IEnumerator<GeoNearHit> GetEnumerator() {
                return GetEnumeratorImplementation();
            }
            #endregion

            #region protected methods
            /// <summary>
            /// Gets the enumerator.
            /// </summary>
            /// <returns>An enumerator.</returns>
            protected abstract IEnumerator<GeoNearHit> GetEnumeratorImplementation();

            /// <summary>
            /// Gets an individual hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            protected abstract GeoNearHit GetHitImplementation(
                int index
            );
            #endregion

            #region explicit interface implementations
            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumeratorImplementation();
            }
            #endregion
        }

        /// <summary>
        /// Represents a GeoNear hit.
        /// </summary>
        public abstract class GeoNearHit {
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
            public object Document {
                get { return DocumentImplementation; }
            }

            /// <summary>
            /// Gets the document as a BsonDocument.
            /// </summary>
            public BsonDocument RawDocument {
                get { return hit["obj"].AsBsonDocument; }
            }
            #endregion

            #region protected properties
            /// <summary>
            /// Gets the document.
            /// </summary>
            protected abstract object DocumentImplementation { get; }
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

    /// <summary>
    /// Represents the result of a GeoNear command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the returned documents.</typeparam>
    [Serializable]
    public class GeoNearResult<TDocument> : GeoNearResult {
        #region private fields
        private GeoNearHits hits;
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
        public new GeoNearHits Hits {
            get {
                if (hits == null) {
                    hits = new GeoNearHits(response["results"].AsBsonArray);
                }
                return hits;
            }
        }
        #endregion

        #region protected properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        protected override GeoNearResult.GeoNearHits HitsImplementation {
            get { return Hits; }
        }
        #endregion

        #region nested classes
        /// <summary>
        /// Represents a collection of GeoNear hits.
        /// </summary>
        public new class GeoNearHits : GeoNearResult.GeoNearHits, IEnumerable<GeoNearHit> {
            #region private fields
            private List<GeoNearHit> hits;
            #endregion

            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHits class.
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
            public override int Count {
                get { return hits.Count; }
            }
            #endregion

            #region indexers
            /// <summary>
            /// Gets an individual hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            public new GeoNearHit this[
                int index
            ] {
                get { return hits[index]; }
            }
            #endregion

            #region public methods
            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            public new IEnumerator<GeoNearHit> GetEnumerator() {
                return hits.GetEnumerator();
            }
            #endregion

            #region protected methods
            /// <summary>
            /// Gets a hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            protected override GeoNearResult.GeoNearHit GetHitImplementation(int index) {
                return hits[index];
            }

            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            protected override IEnumerator<GeoNearResult.GeoNearHit> GetEnumeratorImplementation() {
                return hits.Cast<GeoNearResult.GeoNearHit>().GetEnumerator();
            }
            #endregion
        }

        /// <summary>
        /// Represents a GeoNear hit.
        /// </summary>
        public new class GeoNearHit : GeoNearResult.GeoNearHit {
            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHit class.
            /// </summary>
            /// <param name="hit">The hit.</param>
            public GeoNearHit(
                BsonDocument hit
            )
                : base(hit) {
            }
            #endregion

            #region public properties
            /// <summary>
            /// Gets the document.
            /// </summary>
            public new TDocument Document {
                get {
                    if (typeof(TDocument) == typeof(BsonDocument)) {
                        return (TDocument) (object) RawDocument;
                    } else {
                        return BsonSerializer.Deserialize<TDocument>(RawDocument);
                    }
                }
            }
            #endregion

            #region protected properties
            /// <summary>
            /// Gets the document.
            /// </summary>
            protected override object DocumentImplementation {
                get { return Document;  }
            }
            #endregion
        }
        #endregion
    }
}
