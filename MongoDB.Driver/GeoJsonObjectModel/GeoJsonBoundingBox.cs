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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel
{
    [BsonSerializer(typeof(GeoJsonBoundingBoxSerializer<>))]
    public class GeoJsonBoundingBox<TCoordinates> where TCoordinates : GeoJsonCoordinates
    {
        // private fields
        private TCoordinates _max;
        private TCoordinates _min;

        // constructors
        public GeoJsonBoundingBox(TCoordinates min, TCoordinates max)
        {
            if (min == null)
            {
                throw new ArgumentNullException("min");
            }
            if (max == null)
            {
                throw new ArgumentNullException("max");
            }

            _min = min;
            _max = max;
        }

        // public properties
        public TCoordinates Max
        {
            get { return _max; }
        }

        public TCoordinates Min
        {
            get { return _min; }
        }
    }
}
