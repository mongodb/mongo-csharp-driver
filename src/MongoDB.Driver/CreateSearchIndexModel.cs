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

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Model for creating a search index.
    /// </summary>
    public sealed class CreateSearchIndexModel
    {
        /// <summary>Gets the index name.</summary>
        /// <value>The index name.</value>
        public string Name { get; }

        /// <summary>Gets the index type.</summary>
        /// <value>The index type.</value>
        public SearchIndexType? Type { get; }

        /// <summary>Gets the index definition.</summary>
        /// <value>The definition.</value>
        public BsonDocument Definition { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexModel"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="definition">The definition.</param>
        public CreateSearchIndexModel(string name, BsonDocument definition) : this(name, null, definition) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexModel"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="definition">The definition.</param>
        public CreateSearchIndexModel(string name, SearchIndexType? type, BsonDocument definition)
        {
            Name = name;
            Type = type;
            Definition = definition;
        }
    }
}
