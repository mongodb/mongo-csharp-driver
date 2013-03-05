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
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel.Serializers;
using MongoDB.Shared;

namespace MongoDB.Driver.GeoJsonObjectModel
{
    [BsonSerializer(typeof(GeoJsonCoordinatesSerializer))]
    public abstract class GeoJsonCoordinates : IEquatable<GeoJsonCoordinates>
    {
        // public properties
        public abstract ReadOnlyCollection<double> Values { get; }

        // public operators
        public static bool operator ==(GeoJsonCoordinates lhs, GeoJsonCoordinates rhs)
        {
            return object.Equals(lhs, rhs); // handles lhs == null correctly
        }

        public static bool operator !=(GeoJsonCoordinates lhs, GeoJsonCoordinates rhs)
        {
            return !(lhs == rhs);
        }

        // public methods
        public bool Equals(GeoJsonCoordinates obj)
        {
            return Equals((object)obj); // handles obj == null correctly
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || GetType() != obj.GetType()) { return false; }
            var rhs = (GeoJsonCoordinates)obj;
            return Values.SequenceEqual(rhs.Values);
        }

        public override int GetHashCode()
        {
            return new Hasher()
                .Hash(GetType().GetHashCode())
                .HashElements(Values)
                .GetHashCode();
        }
    }
}
