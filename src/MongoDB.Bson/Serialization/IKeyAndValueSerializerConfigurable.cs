/* Copyright 2010-present MongoDB Inc.
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

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents a serializer that has a key and a value serializer that configuration attributes can be forwarded to.
    /// </summary>
    public interface IKeyAndValueSerializerConfigurable : IBsonDictionarySerializer
    {
        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified key and value serializers.
        /// </summary>
        /// <param name="keySerializer">The key serializer.</param>
        /// <param name="valueSerializer">The value serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        IBsonSerializer WithKeyAndValueSerializers(IBsonSerializer keySerializer, IBsonSerializer valueSerializer);
    }
}