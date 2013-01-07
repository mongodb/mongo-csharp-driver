/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the result of a GeoNear command.
    /// </summary>
    [Serializable]
    public abstract class GeoNearResult : CommandResult
    {
        // private fields
        private GeoNearStats _stats;

        // constructors
        /// <summary>
        /// Initializes a new instance of the GeoNearResult class.
        /// </summary>
        protected GeoNearResult()
        {
        }

        // public properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        public GeoNearHits Hits
        {
            get { return HitsImplementation; }
        }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public string Namespace
        {
            get { return Response["ns"].AsString; }
        }

        /// <summary>
        /// Gets the stats.
        /// </summary>
        public GeoNearStats Stats
        {
            get
            {
                if (_stats == null)
                {
                    _stats = new GeoNearStats(Response["stats"].AsBsonDocument);
                }
                return _stats;
            }
        }

        // protected properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        protected abstract GeoNearHits HitsImplementation { get; }

        // nested classes
        /// <summary>
        /// Represents a collection of GeoNear hits.
        /// </summary>
        public abstract class GeoNearHits : IEnumerable
        {
            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHits class.
            /// </summary>
            protected GeoNearHits()
            {
            }

            // public properties
            /// <summary>
            /// Gets the count of the number of hits.
            /// </summary>
            public abstract int Count { get; }

            // indexers
            /// <summary>
            /// Gets an individual hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            public GeoNearHit this[int index]
            {
                get
                {
                    return GetHitImplementation(index);
                }
            }

            // public methods
            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            public IEnumerator<GeoNearHit> GetEnumerator()
            {
                return GetEnumeratorImplementation();
            }

            // protected methods
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
            protected abstract GeoNearHit GetHitImplementation(int index);

            // explicit interface implementations
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumeratorImplementation();
            }
        }

        /// <summary>
        /// Represents a GeoNear hit.
        /// </summary>
        public abstract class GeoNearHit
        {
            // private fields
            private BsonDocument _hit;

            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHit class.
            /// </summary>
            /// <param name="hit">The hit.</param>
            public GeoNearHit(BsonDocument hit)
            {
                _hit = hit;
            }

            // public properties
            /// <summary>
            /// Gets the distance.
            /// </summary>
            public double Distance
            {
                get { return _hit["dis"].ToDouble(); }
            }

            /// <summary>
            /// Gets the document.
            /// </summary>
            public object Document
            {
                get { return DocumentImplementation; }
            }

            /// <summary>
            /// Gets the document as a BsonDocument.
            /// </summary>
            public BsonDocument RawDocument
            {
                get { return _hit["obj"].AsBsonDocument; }
            }

            // protected properties
            /// <summary>
            /// Gets the document.
            /// </summary>
            protected abstract object DocumentImplementation { get; }
        }

        /// <summary>
        /// Represents the stats of a GeoNear command.
        /// </summary>
        public class GeoNearStats
        {
            // private fields
            private BsonDocument _stats;

            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearStats class.
            /// </summary>
            /// <param name="stats">The stats.</param>
            public GeoNearStats(BsonDocument stats)
            {
                _stats = stats;
            }

            // public properties
            /// <summary>
            /// Gets the average distance.
            /// </summary>
            public double AverageDistance
            {
                get { return _stats["avgDistance"].ToDouble(); }
            }

            /// <summary>
            /// Gets the count of b-tree locations.
            /// </summary>
            public int BTreeLocations
            {
                get { return _stats["btreelocs"].ToInt32(); }
            }

            /// <summary>
            /// Gets the duration.
            /// </summary>
            public TimeSpan Duration
            {
                get { return TimeSpan.FromMilliseconds(_stats["time"].ToInt32()); }
            }

            /// <summary>
            /// Gets the max distance.
            /// </summary>
            public double MaxDistance
            {
                get { return _stats["maxDistance"].ToDouble(); }
            }

            /// <summary>
            /// Gets the number of documents scanned.
            /// </summary>
            public int NumberScanned
            {
                get { return _stats["nscanned"].ToInt32(); }
            }

            /// <summary>
            /// Gets the number of documents loaded.
            /// </summary>
            public int ObjectsLoaded
            {
                get { return _stats["objectsLoaded"].ToInt32(); }
            }
        }
    }

    /// <summary>
    /// Represents the result of a GeoNear command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the returned documents.</typeparam>
    [Serializable]
    public class GeoNearResult<TDocument> : GeoNearResult
    {
        // private fields
        private GeoNearHits _hits;

        // constructors
        /// <summary>
        /// Initializes a new instance of the GeoNearResult class.
        /// </summary>
        public GeoNearResult()
        {
        }

        // public properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        public new GeoNearHits Hits
        {
            get
            {
                if (_hits == null)
                {
                    _hits = new GeoNearHits(Response["results"].AsBsonArray);
                }
                return _hits;
            }
        }

        // protected properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        protected override GeoNearResult.GeoNearHits HitsImplementation
        {
            get { return Hits; }
        }

        // nested classes
        /// <summary>
        /// Represents a collection of GeoNear hits.
        /// </summary>
        public new class GeoNearHits : GeoNearResult.GeoNearHits, IEnumerable<GeoNearHit>
        {
            // private fields
            private List<GeoNearHit> _hits;

            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHits class.
            /// </summary>
            /// <param name="hits">The hits.</param>
            public GeoNearHits(BsonArray hits)
            {
                _hits = hits.Select(h => new GeoNearHit(h.AsBsonDocument)).ToList();
            }

            // public properties
            /// <summary>
            /// Gets the count of the number of hits.
            /// </summary>
            public override int Count
            {
                get { return _hits.Count; }
            }

            // indexers
            /// <summary>
            /// Gets an individual hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            public new GeoNearHit this[int index]
            {
                get { return _hits[index]; }
            }

            // public methods
            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            public new IEnumerator<GeoNearHit> GetEnumerator()
            {
                return _hits.GetEnumerator();
            }

            // protected methods
            /// <summary>
            /// Gets a hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            protected override GeoNearResult.GeoNearHit GetHitImplementation(int index)
            {
                return _hits[index];
            }

            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            protected override IEnumerator<GeoNearResult.GeoNearHit> GetEnumeratorImplementation()
            {
                return _hits.Cast<GeoNearResult.GeoNearHit>().GetEnumerator();
            }
        }

        /// <summary>
        /// Represents a GeoNear hit.
        /// </summary>
        public new class GeoNearHit : GeoNearResult.GeoNearHit
        {
            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoNearHit class.
            /// </summary>
            /// <param name="hit">The hit.</param>
            public GeoNearHit(BsonDocument hit)
                : base(hit)
            {
            }

            // public properties
            /// <summary>
            /// Gets the document.
            /// </summary>
            public new TDocument Document
            {
                get
                {
                    if (typeof(TDocument) == typeof(BsonDocument))
                    {
                        return (TDocument)(object)RawDocument;
                    }
                    else
                    {
                        return BsonSerializer.Deserialize<TDocument>(RawDocument);
                    }
                }
            }

            // protected properties
            /// <summary>
            /// Gets the document.
            /// </summary>
            protected override object DocumentImplementation
            {
                get { return Document; }
            }
        }
    }
}
