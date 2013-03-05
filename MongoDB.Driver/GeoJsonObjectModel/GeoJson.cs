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

namespace MongoDB.Driver.GeoJsonObjectModel
{
    public static class GeoJson
    {
        // public static methods
        public static GeoJsonBoundingBox<TCoordinates> BoundingBox<TCoordinates>(TCoordinates min, TCoordinates max) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonBoundingBox<TCoordinates>(min, max);
        }

        public static GeoJsonFeature<TCoordinates> Feature<TCoordinates>(GeoJsonGeometry<TCoordinates> geometry) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonFeature<TCoordinates>(geometry);
        }

        public static GeoJsonFeature<TCoordinates> Feature<TCoordinates>(GeoJsonFeatureArgs<TCoordinates> args, GeoJsonGeometry<TCoordinates> geometry) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonFeature<TCoordinates>(args, geometry);
        }

        public static GeoJsonFeatureCollection<TCoordinates> FeatureCollection<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, params GeoJsonFeature<TCoordinates>[] features) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonFeatureCollection<TCoordinates>(args, features);
        }

        public static GeoJsonFeatureCollection<TCoordinates> FeatureCollection<TCoordinates>(params GeoJsonFeature<TCoordinates>[] features) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonFeatureCollection<TCoordinates>(features);
        }

        public static GeoJson2DGeographicCoordinates Geographic(double longitude, double latitude)
        {
            return new GeoJson2DGeographicCoordinates(longitude, latitude);
        }

        public static GeoJson3DGeographicCoordinates Geographic(double longitude, double latitude, double altitude)
        {
            return new GeoJson3DGeographicCoordinates(longitude, latitude, altitude);
        }

        public static GeoJsonGeometryCollection<TCoordinates> GeometryCollection<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, params GeoJsonGeometry<TCoordinates>[] geometries) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonGeometryCollection<TCoordinates>(args, geometries);
        }

        public static GeoJsonGeometryCollection<TCoordinates> GeometryCollection<TCoordinates>(params GeoJsonGeometry<TCoordinates>[] geometries) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonGeometryCollection<TCoordinates>(geometries);
        }

        public static GeoJsonLinearRingCoordinates<TCoordinates> LinearRingCoordinates<TCoordinates>(params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonLinearRingCoordinates<TCoordinates>(positions);
        }

        public static GeoJsonLineString<TCoordinates> LineString<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            var coordinates = new GeoJsonLineStringCoordinates<TCoordinates>(positions);
            return new GeoJsonLineString<TCoordinates>(args, coordinates);
        }

        public static GeoJsonLineString<TCoordinates> LineString<TCoordinates>(params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            var coordinates = new GeoJsonLineStringCoordinates<TCoordinates>(positions);
            return new GeoJsonLineString<TCoordinates>(coordinates);
        }

        public static GeoJsonLineStringCoordinates<TCoordinates> LineStringCoordinates<TCoordinates>(params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonLineStringCoordinates<TCoordinates>(positions);
        }

        public static GeoJsonMultiLineString<TCoordinates> MultiLineString<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, params GeoJsonLineStringCoordinates<TCoordinates>[] lineStrings) where TCoordinates : GeoJsonCoordinates
        {
            var coordinates = new GeoJsonMultiLineStringCoordinates<TCoordinates>(lineStrings);
            return new GeoJsonMultiLineString<TCoordinates>(args, coordinates);
        }

        public static GeoJsonMultiLineString<TCoordinates> MultiLineString<TCoordinates>(params GeoJsonLineStringCoordinates<TCoordinates>[] lineStrings) where TCoordinates : GeoJsonCoordinates
        {
            var coordinates = new GeoJsonMultiLineStringCoordinates<TCoordinates>(lineStrings);
            return new GeoJsonMultiLineString<TCoordinates>(coordinates);
        }

        public static GeoJsonMultiPoint<TCoordinates> MultiPoint<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            var coordinates = new GeoJsonMultiPointCoordinates<TCoordinates>(positions);
            return new GeoJsonMultiPoint<TCoordinates>(args, coordinates);
        }

        public static GeoJsonMultiPoint<TCoordinates> MultiPoint<TCoordinates>(params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            var coordinates = new GeoJsonMultiPointCoordinates<TCoordinates>(positions);
            return new GeoJsonMultiPoint<TCoordinates>(coordinates);
        }

        public static GeoJsonMultiPolygon<TCoordinates> MultiPolygon<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, params GeoJsonPolygonCoordinates<TCoordinates>[] polygons) where TCoordinates : GeoJsonCoordinates
        {
            var coordinates = new GeoJsonMultiPolygonCoordinates<TCoordinates>(polygons);
            return new GeoJsonMultiPolygon<TCoordinates>(args, coordinates);
        }

        public static GeoJsonMultiPolygon<TCoordinates> MultiPolygon<TCoordinates>(params GeoJsonPolygonCoordinates<TCoordinates>[] polygons) where TCoordinates : GeoJsonCoordinates
        {
            var coordinates = new GeoJsonMultiPolygonCoordinates<TCoordinates>(polygons);
            return new GeoJsonMultiPolygon<TCoordinates>(coordinates);
        }

        public static GeoJsonPoint<TCoordinates> Point<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, TCoordinates coordinates) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonPoint<TCoordinates>(args, coordinates);
        }

        public static GeoJsonPoint<TCoordinates> Point<TCoordinates>(TCoordinates coordinates) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonPoint<TCoordinates>(coordinates);
        }

        public static GeoJsonPolygon<TCoordinates> Polygon<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            var exterior = new GeoJsonLinearRingCoordinates<TCoordinates>(positions);
            var coordinates = new GeoJsonPolygonCoordinates<TCoordinates>(exterior);
            return new GeoJsonPolygon<TCoordinates>(args, coordinates);
        }

        public static GeoJsonPolygon<TCoordinates> Polygon<TCoordinates>(GeoJsonObjectArgs<TCoordinates> args, GeoJsonPolygonCoordinates<TCoordinates> coordinates) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonPolygon<TCoordinates>(args, coordinates);
        }

        public static GeoJsonPolygon<TCoordinates> Polygon<TCoordinates>(GeoJsonPolygonCoordinates<TCoordinates> coordinates) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonPolygon<TCoordinates>(coordinates);
        }

        public static GeoJsonPolygon<TCoordinates> Polygon<TCoordinates>(params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            var exterior = new GeoJsonLinearRingCoordinates<TCoordinates>(positions);
            var coordinates = new GeoJsonPolygonCoordinates<TCoordinates>(exterior);
            return new GeoJsonPolygon<TCoordinates>(coordinates);
        }

        public static GeoJsonPolygonCoordinates<TCoordinates> PolygonCoordinates<TCoordinates>(params TCoordinates[] positions) where TCoordinates : GeoJsonCoordinates
        {
            var exterior = new GeoJsonLinearRingCoordinates<TCoordinates>(positions);
            return new GeoJsonPolygonCoordinates<TCoordinates>(exterior);
        }

        public static GeoJsonPolygonCoordinates<TCoordinates> PolygonCoordinates<TCoordinates>(GeoJsonLinearRingCoordinates<TCoordinates> exterior, params GeoJsonLinearRingCoordinates<TCoordinates>[] holes) where TCoordinates : GeoJsonCoordinates
        {
            return new GeoJsonPolygonCoordinates<TCoordinates>(exterior, holes);
        }

        public static GeoJson2DCoordinates Position(double x, double y)
        {
            return new GeoJson2DCoordinates(x, y);
        }

        public static GeoJson3DCoordinates Position(double x, double y, double z)
        {
            return new GeoJson3DCoordinates(x, y, z);
        }

        public static GeoJson2DProjectedCoordinates Projected(double easting, double northing)
        {
            return new GeoJson2DProjectedCoordinates(easting, northing);
        }

        public static GeoJson3DProjectedCoordinates Projected(double easting, double northing, double altitude)
        {
            return new GeoJson3DProjectedCoordinates(easting, northing, altitude);
        }
    }
}
