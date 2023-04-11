/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.GeoJsonObjectModel;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// Base class for area argument for GeoWithin queries.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public abstract class GeoWithinArea<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        internal abstract BsonElement Render();
    }

    /// <summary>
    /// Object that specifies the bottom left and top right GeoJSON points of a box to
    /// search within.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public sealed class GeoWithinBox<TCoordinates> : GeoWithinArea<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoWithinBox{TCoordinates}"/> class.
        /// </summary>
        /// <param name="bottomLeft">The bottom left GeoJSON point.</param>
        /// <param name="topRight">The top right GeoJSON point.</param>
        public GeoWithinBox(GeoJsonPoint<TCoordinates> bottomLeft, GeoJsonPoint<TCoordinates> topRight)
        {
            BottomLeft = Ensure.IsNotNull(bottomLeft, nameof(bottomLeft));
            TopRight = Ensure.IsNotNull(topRight, nameof(topRight));
        }

        /// <summary> Gets the bottom left GeoJSON point.</summary>
        public GeoJsonPoint<TCoordinates> BottomLeft { get; }

        /// <summary> Gets the top right GeoJSON point.</summary>
        public GeoJsonPoint<TCoordinates> TopRight { get; }

        internal override BsonElement Render() =>
            new("box", new BsonDocument
                {
                    { "bottomLeft", BottomLeft.ToBsonDocument() },
                    { "topRight", TopRight.ToBsonDocument() }
                });
    }

    /// <summary>
    /// Object that specifies the center point and the radius in meters to search within.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public sealed class GeoWithinCircle<TCoordinates> : GeoWithinArea<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoWithinCircle{TCoordinates}"/> class.
        /// </summary>
        /// <param name="center">Center of the circle specified as a GeoJSON point.</param>
        /// <param name="radius">Radius specified in meters.</param>
        public GeoWithinCircle(GeoJsonPoint<TCoordinates> center, double radius)
        {
            Center = Ensure.IsNotNull(center, nameof(center));
            Radius = Ensure.IsGreaterThanZero(radius, nameof(radius));
        }

        /// <summary>Gets the center of the circle specified as a GeoJSON point.</summary>
        public GeoJsonPoint<TCoordinates> Center { get; }

        /// <summary>Gets the radius specified in meters.</summary>
        public double Radius { get; }

        internal override BsonElement Render() =>
            new("circle", new BsonDocument
               {
                    { "center", Center.ToBsonDocument() },
                    { "radius", Radius }
               });
    }

    /// <summary>
    /// Object that specifies the GeoJson geometry to search within.
    /// </summary>
    /// <typeparam name="TCoordinates">The type of the coordinates.</typeparam>
    public sealed class GeoWithinGeometry<TCoordinates> : GeoWithinArea<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoWithinBox{TCoordinates}"/> class.
        /// </summary>
        /// <param name="geometry">GeoJSON object specifying the MultiPolygon or Polygon.</param>
        public GeoWithinGeometry(GeoJsonGeometry<TCoordinates> geometry)
        {
            Geometry = Ensure.IsNotNull(geometry, nameof(geometry));
        }

        /// <summary>Gets the GeoJson geometry.</summary>
        public GeoJsonGeometry<TCoordinates> Geometry { get; }

        internal override BsonElement Render() => new("geometry", Geometry.ToBsonDocument());
    }
}
