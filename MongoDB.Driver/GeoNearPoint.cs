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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.GeoJsonObjectModel.Serializers;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a GeoNearLocation (either a legacy coordinate pair or a GeoJsonPoint).
    /// </summary>
    public abstract class GeoNearPoint
    {
        // public static methods
        /// <summary>
        /// Creates a GeoNearLocation.GeoJson{TCoordinates} from a GeoJsonPoint{TCoordinates}.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        /// <param name="point">The GeoJson point.</param>
        /// <returns>A GeoNearLocation.GeoJson{TCoordinates}.</returns>
        public static GeoNearPoint.GeoJson<TCoordinates> From<TCoordinates>(GeoJsonPoint<TCoordinates> point) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJson<TCoordinates>(point);
        }

        /// <summary>
        /// Creates a GeoNearPoint from legacy x and y coordinates.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns></returns>
        public static GeoNearPoint.Legacy From(double x, double y)
        {
            return new Legacy(x, y);
        }

        // public methods
        /// <summary>
        /// Converts the GeoNearLocation into a BsonValue for the GeoNear command.
        /// </summary>
        /// <returns></returns>
        public abstract BsonValue ToGeoNearCommandField();

        // nested classes
        /// <summary>
        /// Represents a GeoNearLocation expressed as a legacy coordinate pair.
        /// </summary>
        public class Legacy : GeoNearPoint
        {
            // private fields
            private readonly double _x;
            private readonly double _y;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="Legacy"/> class.
            /// </summary>
            /// <param name="x">The x value.</param>
            /// <param name="y">The y value.</param>
            public Legacy(double x, double y)
            {
                _x = x;
                _y = y;
            }

            // public properties
            /// <summary>
            /// Gets the X value.
            /// </summary>
            /// <value>
            /// The X value.
            /// </value>
            public double X
            {
                get { return _x; }
            }

            /// <summary>
            /// Gets the Y value.
            /// </summary>
            /// <value>
            /// The Y value.
            /// </value>
            public double Y
            {
                get { return _y; }
            }

            // public methods
            /// <summary>
            /// Converts the GeoNearLocation into a BsonValue for the GeoNear command.
            /// </summary>
            /// <returns></returns>
            public override BsonValue ToGeoNearCommandField()
            {
                return new BsonArray { _x, _y };
            }
        }

        /// <summary>
        /// Represents a GeoNearLocation expressed as a GeoJsonPoint.
        /// </summary>
        /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
        public class GeoJson<TCoordinates> : GeoNearPoint where TCoordinates : GeoJsonCoordinates
        {
            // private fields
            private readonly GeoJsonPoint<TCoordinates> _point;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="GeoJson{TCoordinates}"/> class.
            /// </summary>
            /// <param name="point">The point.</param>
            public GeoJson(GeoJsonPoint<TCoordinates> point)
            {
                _point = point;
            }

            // public properties
            /// <summary>
            /// Gets the point.
            /// </summary>
            /// <value>
            /// The point.
            /// </value>
            public GeoJsonPoint<TCoordinates> Point
            {
                get { return _point; }
            }

            // public methods
            /// <summary>
            /// Converts the GeoNearLocation into a BsonValue for the GeoNear command.
            /// </summary>
            /// <returns></returns>
            public override BsonValue ToGeoNearCommandField()
            {
                var document = new BsonDocument();
                using (var writer = new BsonDocumentWriter(document, BsonDocumentWriterSettings.Defaults))
                {
                    new GeoJsonPointSerializer<TCoordinates>().Serialize(writer, typeof(GeoJsonPoint<TCoordinates>), _point, null);
                }
                return document;
            }
        }
    }
}
