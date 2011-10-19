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
    /// Represents the result of a GeoHaystackSearch command.
    /// </summary>
    [Serializable]
    public abstract class GeoHaystackSearchResult : CommandResult {
        #region private fields
        private GeoHaystackSearchStats stats;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the GeoHaystackSearchResult class.
        /// </summary>
        protected GeoHaystackSearchResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        public GeoHaystackSearchHits Hits {
            get { return HitsImplementation; }
        }

        /// <summary>
        /// Gets the stats.
        /// </summary>
        public GeoHaystackSearchStats Stats {
            get {
                if (stats == null) {
                    stats = new GeoHaystackSearchStats(response["stats"].AsBsonDocument);
                }
                return stats;
            }
        }
        #endregion

        #region protected properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        protected abstract GeoHaystackSearchHits HitsImplementation { get; }
        #endregion

        #region nested classes
        /// <summary>
        /// Represents a collection of GeoHaystackSearch hits.
        /// </summary>
        public abstract class GeoHaystackSearchHits : IEnumerable {
            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchHits class.
            /// </summary>
            protected GeoHaystackSearchHits() {
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
            public GeoHaystackSearchHit this[
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
            public IEnumerator<GeoHaystackSearchHit> GetEnumerator() {
                return GetEnumeratorImplementation();
            }
            #endregion

            #region protected methods
            /// <summary>
            /// Gets the enumerator.
            /// </summary>
            /// <returns>An enumerator.</returns>
            protected abstract IEnumerator<GeoHaystackSearchHit> GetEnumeratorImplementation();

            /// <summary>
            /// Gets an individual hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            protected abstract GeoHaystackSearchHit GetHitImplementation(
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
        /// Represents a GeoHaystackSearch hit.
        /// </summary>
        public abstract class GeoHaystackSearchHit {
            #region private fields
            private BsonDocument hit;
            #endregion

            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchHit class.
            /// </summary>
            /// <param name="hit">The hit.</param>
            public GeoHaystackSearchHit(
                BsonDocument hit
            ) {
                this.hit = hit;
            }
            #endregion

            #region public properties
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
                get { return hit; }
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
        /// Represents the stats of a GeoHaystackSearch command.
        /// </summary>
        public class GeoHaystackSearchStats {
            #region private fields
            private BsonDocument stats;
            #endregion

            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchStats class.
            /// </summary>
            /// <param name="stats">The stats.</param>
            public GeoHaystackSearchStats(
                BsonDocument stats
            ) {
                this.stats = stats;
            }
            #endregion

            #region public properties
            /// <summary>
            /// Gets the count of b-tree matches.
            /// </summary>
            public int BTreeMatches {
                get { return stats["btreeMatches"].ToInt32(); }
            }

            /// <summary>
            /// Gets the duration.
            /// </summary>
            public TimeSpan Duration {
                get { return TimeSpan.FromMilliseconds(stats["time"].ToInt32()); }
            }

            /// <summary>
            /// Gets the number of hits.
            /// </summary>
            public int NumberOfHits {
                get { return stats["n"].ToInt32(); }
            }
            #endregion
        }
        #endregion
    }

    /// <summary>
    /// Represents the result of a GeoHaystackSearch command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the returned documents.</typeparam>
    [Serializable]
    public class GeoHaystackSearchResult<TDocument> : GeoHaystackSearchResult {
        #region private fields
        private GeoHaystackSearchHits hits;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the GeoHaystackSearchResult class.
        /// </summary>
        public GeoHaystackSearchResult() {
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        public new GeoHaystackSearchHits Hits {
            get {
                if (hits == null) {
                    hits = new GeoHaystackSearchHits(response["results"].AsBsonArray);
                }
                return hits;
            }
        }
        #endregion

        #region protected properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        protected override GeoHaystackSearchResult.GeoHaystackSearchHits HitsImplementation {
            get { return Hits; }
        }
        #endregion

        #region nested classes
        /// <summary>
        /// Represents a collection of GeoHaystackSearch hits.
        /// </summary>
        public new class GeoHaystackSearchHits : GeoHaystackSearchResult.GeoHaystackSearchHits, IEnumerable<GeoHaystackSearchHit> {
            #region private fields
            private List<GeoHaystackSearchHit> hits;
            #endregion

            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchHits class.
            /// </summary>
            /// <param name="hits">The hits.</param>
            public GeoHaystackSearchHits(
                BsonArray hits
            ) {
                this.hits = hits.Select(h => new GeoHaystackSearchHit(h.AsBsonDocument)).ToList();
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
            public new GeoHaystackSearchHit this[
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
            public new IEnumerator<GeoHaystackSearchHit> GetEnumerator() {
                return hits.GetEnumerator();
            }
            #endregion

            #region protected methods
            /// <summary>
            /// Gets a hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            protected override GeoHaystackSearchResult.GeoHaystackSearchHit GetHitImplementation(int index) {
                return hits[index];
            }

            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            protected override IEnumerator<GeoHaystackSearchResult.GeoHaystackSearchHit> GetEnumeratorImplementation() {
                return hits.Cast<GeoHaystackSearchResult.GeoHaystackSearchHit>().GetEnumerator();
            }
            #endregion
        }

        /// <summary>
        /// Represents a GeoHaystackSearch hit.
        /// </summary>
        public new class GeoHaystackSearchHit : GeoHaystackSearchResult.GeoHaystackSearchHit {
            #region constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchHit class.
            /// </summary>
            /// <param name="hit">The hit.</param>
            public GeoHaystackSearchHit(
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
                get { return Document; }
            }
            #endregion
        }
        #endregion
    }
}
