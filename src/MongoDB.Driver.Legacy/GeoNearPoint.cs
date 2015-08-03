/* Copyright 2010-2015 MongoDB Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.GeoJsonObjectModel.Serializers;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a GeoNearPoint (wraps either an XYPoint or a GeoJsonPoint).
    /// </summary>
    public abstract class GeoNearPoint
    {
        // implicit conversions
        /// <summary>
        /// Implicit conversion to wrap an XYPoint in a GeoNearPoint value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A GeoNearPoint value.</returns>
        public static implicit operator GeoNearPoint(XYPoint value)
        {
            return new Legacy(value);
        }

        /// <summary>
        /// Implicit conversion to wrap a 2D GeoJson point in a GeoNearPoint value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A GeoNearPoint value.</returns>
        public static implicit operator GeoNearPoint(GeoJsonPoint<GeoJson2DCoordinates> value)
        {
            return new GeoJson<GeoJson2DCoordinates>(value);
        }

        /// <summary>
        /// Implicit conversion to wrap a 2D GeoJson point in a GeoNearPoint value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A GeoNearPoint value.</returns>
        public static implicit operator GeoNearPoint(GeoJsonPoint<GeoJson2DGeographicCoordinates> value)
        {
            return new GeoJson<GeoJson2DGeographicCoordinates>(value);
        }

        /// <summary>
        /// Implicit conversion to wrap a 2D GeoJson point in a GeoNearPoint value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A GeoNearPoint value.</returns>
        public static implicit operator GeoNearPoint(GeoJsonPoint<GeoJson2DProjectedCoordinates> value)
        {
            return new GeoJson<GeoJson2DProjectedCoordinates>(value);
        }

        // internal methods
        /// <summary>
        /// Converts the GeoNearPoint into a BsonValue for the GeoNear command.
        /// </summary>
        /// <returns>A BsonValue.</returns>
        internal abstract BsonValue ToGeoNearCommandValue();

        // nested classes
        /// <summary>
        /// Represents a GeoNearPoint that wraps an XYPoint.
        /// </summary>
        public class Legacy : GeoNearPoint
        {
            // private fields
            private readonly XYPoint _value;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="Legacy"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public Legacy(XYPoint value)
            {
                _value = value;
            }

            // public properties
            /// <summary>
            /// Gets the value.
            /// </summary>
            public XYPoint Value
            {
                get { return _value; }
            }

            // internal methods
            /// <summary>
            /// Converts the GeoNearPoint into a BsonValue for the GeoNear command.
            /// </summary>
            /// <returns>A BsonValue.</returns>
            internal override BsonValue ToGeoNearCommandValue()
            {
                return new BsonArray { _value.X, _value.Y };
            }
        }

        /// <summary>
        /// Represents a GeoNearPoint that wraps a GeoJsonPoint.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        public class GeoJson<TCoordinates> : GeoNearPoint where TCoordinates : GeoJsonCoordinates
        {
            // private fields
            private readonly GeoJsonPoint<TCoordinates> _value;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="GeoJson{TCoordinates}"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public GeoJson(GeoJsonPoint<TCoordinates> value)
            {
                _value = value;
            }

            // public properties
            /// <summary>
            /// Gets the value.
            /// </summary>
            public GeoJsonPoint<TCoordinates> Value
            {
                get { return _value; }
            }

            // internal methods
            /// <summary>
            /// Converts the GeoNearPoint into a BsonValue for the GeoNear command.
            /// </summary>
            /// <returns>A BsonValue.</returns>
            internal override BsonValue ToGeoNearCommandValue()
            {
                var document = new BsonDocument();
                using (var writer = new BsonDocumentWriter(document, BsonDocumentWriterSettings.Defaults))
                {
                    var context = BsonSerializationContext.CreateRoot(writer);
                    new GeoJsonPointSerializer<TCoordinates>().Serialize(context, _value);
                }
                return document;
            }
        }
    }
}
