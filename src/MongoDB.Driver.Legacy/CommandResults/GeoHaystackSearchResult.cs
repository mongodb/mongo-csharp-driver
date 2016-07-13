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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the result of a GeoHaystackSearch command.
    /// </summary>
#if NET45
    [Serializable]
#endif
    public abstract class GeoHaystackSearchResult : CommandResult
    {
        // private fields
        private GeoHaystackSearchStats _stats;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoHaystackSearchResult"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        protected GeoHaystackSearchResult(BsonDocument response)
            : base(response)
        {
        }

        // public properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        public GeoHaystackSearchHits Hits
        {
            get { return HitsImplementation; }
        }

        /// <summary>
        /// Gets the stats.
        /// </summary>
        public GeoHaystackSearchStats Stats
        {
            get
            {
                if (_stats == null)
                {
                    _stats = new GeoHaystackSearchStats(Response["stats"].AsBsonDocument);
                }
                return _stats;
            }
        }

        // protected properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        protected abstract GeoHaystackSearchHits HitsImplementation { get; }

        // nested classes
        /// <summary>
        /// Represents a collection of GeoHaystackSearch hits.
        /// </summary>
        public abstract class GeoHaystackSearchHits : IEnumerable
        {
            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchHits class.
            /// </summary>
            protected GeoHaystackSearchHits()
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
            public GeoHaystackSearchHit this[int index]
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
            public IEnumerator<GeoHaystackSearchHit> GetEnumerator()
            {
                return GetEnumeratorImplementation();
            }

            // protected methods
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
            protected abstract GeoHaystackSearchHit GetHitImplementation(int index);

            // explicit interface implementations
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumeratorImplementation();
            }
        }

        /// <summary>
        /// Represents a GeoHaystackSearch hit.
        /// </summary>
        public abstract class GeoHaystackSearchHit
        {
            // private fields
            private BsonDocument _hit;

            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchHit class.
            /// </summary>
            /// <param name="hit">The hit.</param>
            public GeoHaystackSearchHit(BsonDocument hit)
            {
                _hit = hit;
            }

            // public properties
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
                get { return _hit; }
            }

            // protected properties
            /// <summary>
            /// Gets the document.
            /// </summary>
            protected abstract object DocumentImplementation { get; }
        }

        /// <summary>
        /// Represents the stats of a GeoHaystackSearch command.
        /// </summary>
        public class GeoHaystackSearchStats
        {
            // private fields
            private BsonDocument _stats;

            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchStats class.
            /// </summary>
            /// <param name="stats">The stats.</param>
            public GeoHaystackSearchStats(BsonDocument stats)
            {
                _stats = stats;
            }

            // public properties
            /// <summary>
            /// Gets the count of b-tree matches.
            /// </summary>
            public int BTreeMatches
            {
                get { return _stats["btreeMatches"].ToInt32(); }
            }

            /// <summary>
            /// Gets the duration.
            /// </summary>
            public TimeSpan Duration
            {
                get { return TimeSpan.FromMilliseconds(_stats["time"].ToInt32()); }
            }

            /// <summary>
            /// Gets the number of hits.
            /// </summary>
            public int NumberOfHits
            {
                get { return _stats["n"].ToInt32(); }
            }
        }
    }

    /// <summary>
    /// Represents the result of a GeoHaystackSearch command.
    /// </summary>
    /// <typeparam name="TDocument">The type of the returned documents.</typeparam>
#if NET45
    [Serializable]
#endif
    [BsonSerializer(typeof(GeoHaystackSearchResult<>.Serializer))]
    public class GeoHaystackSearchResult<TDocument> : GeoHaystackSearchResult
    {
        // private fields
        private GeoHaystackSearchHits _hits;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoHaystackSearchResult{TDocument}"/> class.
        /// </summary>
        /// <param name="response">The response.</param>
        public GeoHaystackSearchResult(BsonDocument response)
            : base(response)
        {
        }

        // public properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        public new GeoHaystackSearchHits Hits
        {
            get
            {
                if (_hits == null)
                {
                    _hits = new GeoHaystackSearchHits(Response["results"].AsBsonArray);
                }
                return _hits;
            }
        }

        // protected properties
        /// <summary>
        /// Gets the hits.
        /// </summary>
        protected override GeoHaystackSearchResult.GeoHaystackSearchHits HitsImplementation
        {
            get { return Hits; }
        }

        // nested classes
        /// <summary>
        /// Represents a collection of GeoHaystackSearch hits.
        /// </summary>
        public new class GeoHaystackSearchHits : GeoHaystackSearchResult.GeoHaystackSearchHits, IEnumerable<GeoHaystackSearchHit>
        {
            // private fields
            private List<GeoHaystackSearchHit> _hits;

            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchHits class.
            /// </summary>
            /// <param name="hits">The hits.</param>
            public GeoHaystackSearchHits(BsonArray hits)
            {
                _hits = hits.Select(h => new GeoHaystackSearchHit(h.AsBsonDocument)).ToList();
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
            public new GeoHaystackSearchHit this[int index]
            {
                get { return _hits[index]; }
            }

            // public methods
            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            public new IEnumerator<GeoHaystackSearchHit> GetEnumerator()
            {
                return _hits.GetEnumerator();
            }

            // protected methods
            /// <summary>
            /// Gets a hit.
            /// </summary>
            /// <param name="index">The zero based index of the hit.</param>
            /// <returns>The hit.</returns>
            protected override GeoHaystackSearchResult.GeoHaystackSearchHit GetHitImplementation(int index)
            {
                return _hits[index];
            }

            /// <summary>
            /// Gets an enumerator for the hits.
            /// </summary>
            /// <returns>An enumerator.</returns>
            protected override IEnumerator<GeoHaystackSearchResult.GeoHaystackSearchHit> GetEnumeratorImplementation()
            {
                return _hits.Cast<GeoHaystackSearchResult.GeoHaystackSearchHit>().GetEnumerator();
            }
        }

        /// <summary>
        /// Represents a GeoHaystackSearch hit.
        /// </summary>
        public new class GeoHaystackSearchHit : GeoHaystackSearchResult.GeoHaystackSearchHit
        {
            // constructors
            /// <summary>
            /// Initializes a new instance of the GeoHaystackSearchHit class.
            /// </summary>
            /// <param name="hit">The hit.</param>
            public GeoHaystackSearchHit(BsonDocument hit)
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

        // nested classes
        internal class Serializer : SerializerBase<GeoHaystackSearchResult<TDocument>>
        {
            private readonly IBsonSerializer<GeoHaystackSearchResult<TDocument>> _serializer = new CommandResultSerializer<GeoHaystackSearchResult<TDocument>>();

            public override GeoHaystackSearchResult<TDocument> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                return _serializer.Deserialize(context);
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GeoHaystackSearchResult<TDocument> value)
            {
                _serializer.Serialize(context, value);
            }
        }
    }
}
