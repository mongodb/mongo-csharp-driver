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
    /// Represents a wrapped object that can be used where an IMongoScope is expected (the wrapped object is expected to serialize properly).
    /// </summary>
    [BsonSerializer(typeof(ScopeWrapper.Serializer))]
    public class ScopeWrapper : BaseWrapper, IMongoScope
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the ScopeWrapper class.
        /// </summary>
        /// <param name="scope">The wrapped object.</param>
        public ScopeWrapper(object scope)
            : base(scope)
        {
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the ScopeWrapper class.
        /// </summary>
        /// <param name="scope">The wrapped object.</param>
        /// <returns>A new instance of ScopeWrapper or null.</returns>
        public static ScopeWrapper Create(object scope)
        {
            if (scope == null)
            {
                return null;
            }
            else
            {
                return new ScopeWrapper(scope);
            }
        }

        // nested classes
        new internal class Serializer : SerializerBase<ScopeWrapper>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ScopeWrapper value)
            {
                value.SerializeWrappedObject(context);
            }
        }
    }
}
