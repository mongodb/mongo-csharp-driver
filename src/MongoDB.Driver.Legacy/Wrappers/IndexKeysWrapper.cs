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

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Wrappers
{
    /// <summary>
    /// Represents a wrapped object that can be used where an IMongoIndexKeys is expected (the wrapped object is expected to serialize properly).
    /// </summary>
    [BsonSerializer(typeof(IndexKeysWrapper.Serializer))]
    public class IndexKeysWrapper : BaseWrapper, IMongoIndexKeys
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the IndexKeysWrapper class.
        /// </summary>
        /// <param name="keys">The wrapped object.</param>
        public IndexKeysWrapper(object keys)
            : base(keys)
        {
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the IndexKeysWrapper class.
        /// </summary>
        /// <param name="keys">The wrapped object.</param>
        /// <returns>A new instance of IndexKeysWrapper or null.</returns>
        public static IndexKeysWrapper Create(object keys)
        {
            if (keys == null)
            {
                return null;
            }
            else
            {
                return new IndexKeysWrapper(keys);
            }
        }

        // nested classes
        new internal class Serializer : SerializerBase<IndexKeysWrapper>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IndexKeysWrapper value)
            {
                value.SerializeWrappedObject(context);
            }
        }
    }
}
