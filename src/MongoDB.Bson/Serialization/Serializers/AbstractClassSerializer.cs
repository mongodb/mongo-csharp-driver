/* Copyright 2010-2014 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a skeleton serializer for an abstract class that simply forwards Serialize to the actual serializer.
    /// </summary>
    /// <typeparam name="TClass">The type of the class.</typeparam>
    public class AbstractClassSerializer<TClass> : BsonBaseSerializer<TClass>
    {
        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="BsonSerializationException"></exception>
        public override void Serialize(BsonSerializationContext context, TClass value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
                return;
            }

            var actualType = value.GetType();
            if (actualType != typeof(TClass))
            {
                var serializer = BsonSerializer.LookupSerializer(actualType);
                serializer.Serialize(context, value);
                return;
            }

            var message = string.Format(
                "{0} is not an abstract class.",
                BsonUtils.GetFriendlyTypeName(actualType));
            throw new BsonSerializationException(message);
        }
    }
}
