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

using System;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Wrappers
{
    /// <summary>
    /// Represents a wrapped object that can be used where an IMongoGeoNearOptions is expected (the wrapped object is expected to serialize properly).
    /// </summary>
    [Obsolete("Use GeoNearArgs instead.")]
    [BsonSerializer(typeof(GeoNearOptionsWrapper.Serializer))]
    public class GeoNearOptionsWrapper : BaseWrapper, IMongoGeoNearOptions
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the GeoNearOptionsWrapper class.
        /// </summary>
        /// <param name="options">The wrapped object.</param>
        public GeoNearOptionsWrapper(object options)
            : base(options)
        {
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the GeoNearOptionsWrapper class.
        /// </summary>
        /// <param name="options">The wrapped object.</param>
        /// <returns>A new instance of GeoNearOptionsWrapper or null.</returns>
        public static GeoNearOptionsWrapper Create(object options)
        {
            if (options == null)
            {
                return null;
            }
            else
            {
                return new GeoNearOptionsWrapper(options);
            }
        }

        // nested classes
        new internal class Serializer : SerializerBase<GeoNearOptionsWrapper>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, GeoNearOptionsWrapper value)
            {
                value.SerializeWrappedObject(context);
            }
        }
    }
}
