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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel
{
    [BsonSerializer(typeof(GeoJsonFeatureSerializer<>))]
    public class GeoJsonFeature<TCoordinates> : GeoJsonObject<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private GeoJsonGeometry<TCoordinates> _geometry;
        private BsonValue _id;
        private BsonDocument _properties;

        // constructors
        public GeoJsonFeature(GeoJsonGeometry<TCoordinates> geometry)
            : this(null, geometry)
        {
        }

        public GeoJsonFeature(GeoJsonFeatureArgs<TCoordinates> args, GeoJsonGeometry<TCoordinates> geometry)
            : base(args)
        {
            _geometry = geometry;

            if (args != null)
            {
                _id = args.Id;
                _properties = args.Properties;
            }
        }

        // public properties
        public GeoJsonGeometry<TCoordinates> Geometry
        {
            get { return _geometry; }
        }

        public BsonValue Id
        {
            get { return _id; }
        }

        public BsonDocument Properties
        {
            get { return _properties; }
        }

        public override GeoJsonObjectType Type
        {
            get { return GeoJsonObjectType.Feature; }
        }
    }
}
