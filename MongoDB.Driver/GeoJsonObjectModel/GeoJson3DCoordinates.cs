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

using System.Collections.ObjectModel;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel.Serializers;

namespace MongoDB.Driver.GeoJsonObjectModel
{
    [BsonSerializer(typeof(GeoJson3DCoordinatesSerializer))]
    public class GeoJson3DCoordinates : GeoJsonCoordinates
    {
        // private fields
        private ReadOnlyCollection<double> _values;

        // constructors
        public GeoJson3DCoordinates(double x, double y, double z)
        {
            _values = new ReadOnlyCollection<double>(new[] { x, y, z });
        }

        // public properties
        public override ReadOnlyCollection<double> Values
        {
            get { return _values; }
        }

        public double X
        {
            get { return _values[0]; }
        }

        public double Y
        {
            get { return _values[1]; }
        }

        public double Z
        {
            get { return _values[2]; }
        }
    }
}
