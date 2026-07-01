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

using MongoDB.Bson.Serialization;

namespace MongoDB.Bson
{
    internal interface IBsonDefaults
    {
        /// <summary>Gets or sets the default serializer for dynamic BSON arrays.</summary>
        IBsonSerializer DynamicArraySerializer { get; set; }

        /// <summary>Gets or sets the default serializer for dynamic BSON documents.</summary>
        IBsonSerializer DynamicDocumentSerializer { get; set; }

        /// <summary>Gets or sets the maximum allowed BSON document size in bytes.</summary>
        int MaxDocumentSize { get; set; }

        /// <summary>Gets or sets the maximum allowed serialization depth.</summary>
        int MaxSerializationDepth { get; set; }
    }
}