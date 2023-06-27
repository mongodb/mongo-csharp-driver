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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a create search index request.
    /// </summary>
    public sealed class CreateSearchIndexRequest
    {
        /// <summary>Gets the index name.</summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>Gets the definition.</summary>
        /// <value>The definition.</value>
        public BsonDocument Definition { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexRequest"/> class.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="definition">The index definition.</param>
        public CreateSearchIndexRequest(string name, BsonDocument definition)
        {
            Name = name;
            Definition = Ensure.IsNotNull(definition, nameof(definition));
        }

        // methods
        internal BsonDocument CreateIndexDocument() =>
            new()
            {
                { "name", Name, Name != null },
                { "definition", Definition }
            };
    }
}
