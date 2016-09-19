/* Copyright 2016 MongoDB Inc.
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
    /// Represents a wrapped object that can be used where an IMongoCreateViewOptions is expected (the wrapped object is expected to serialize properly).
    /// </summary>
    [BsonSerializer(typeof(CreateViewOptionsWrapper.Serializer))]
    public class CreateViewOptionsWrapper : BaseWrapper, IMongoCreateViewOptions
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the CreateViewOptionsWrapper class.
        /// </summary>
        /// <param name="options">The wrapped object.</param>
        public CreateViewOptionsWrapper(object options)
            : base(options)
        {
        }

        // public static methods
        /// <summary>
        /// Creates a new instance of the CreateViewOptionsWrapper class.
        /// </summary>
        /// <param name="options">The wrapped object.</param>
        /// <returns>A new instance of CreateViewOptionsWrapper or null.</returns>
        public static CreateViewOptionsWrapper Create(object options)
        {
            if (options == null)
            {
                return null;
            }
            else
            {
                return new CreateViewOptionsWrapper(options);
            }
        }

        // nested classes
        new internal class Serializer : SerializerBase<CreateViewOptionsWrapper>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CreateViewOptionsWrapper value)
            {
                value.SerializeWrappedObject(context);
            }
        }
    }
}
