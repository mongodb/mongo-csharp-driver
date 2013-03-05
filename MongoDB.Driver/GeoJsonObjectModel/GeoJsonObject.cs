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
    [BsonSerializer(typeof(GeoJsonObjectSerializer<>))]
    public abstract class GeoJsonObject<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private GeoJsonBoundingBox<TCoordinates> _boundingBox;
        private GeoJsonCoordinateReferenceSystem _coordinateReferenceSystem;
        private BsonDocument _extraMembers;

        // constructor
        protected GeoJsonObject(GeoJsonObjectArgs<TCoordinates> args)
        {
            if (args != null)
            {
                _boundingBox = args.BoundingBox;
                _coordinateReferenceSystem = args.CoordinateReferenceSystem;
                _extraMembers = args.ExtraMembers;
            }
        }

        // public properties
        public GeoJsonBoundingBox<TCoordinates> BoundingBox
        {
            get { return _boundingBox; }
        }

        public GeoJsonCoordinateReferenceSystem CoordinateReferenceSystem
        {
            get { return _coordinateReferenceSystem; }
        }

        public BsonDocument ExtraMembers
        {
            get { return _extraMembers; }
        }

        public abstract GeoJsonObjectType Type { get; }
    }
}
