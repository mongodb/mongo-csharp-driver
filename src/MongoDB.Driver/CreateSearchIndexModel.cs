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

using System;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Defines a search index model using a <see cref="BsonDocument"/> definition. Consider using
    /// <see cref="CreateVectorSearchIndexModel{TDocument}"/> to build vector indexes without specifying the BSON directly.
    /// </summary>
    public class CreateSearchIndexModel
    {
        private readonly BsonDocument _definition;
        private readonly SearchIndexType? _type;
        private readonly string _name;

        /// <summary>Gets the index name.</summary>
        /// <value>The index name.</value>
        public string Name => _name;

        /// <summary>Gets the index type.</summary>
        /// <value>The index type.</value>
        public SearchIndexType? Type => _type;

        /// <summary>
        /// Gets the index definition, if one was passed to a constructor of this class, otherwise throws.
        /// </summary>
        /// <value>The definition.</value>
        public BsonDocument Definition
            => _definition ?? throw new NotSupportedException(
                "This method should not be called on this subtype. Instead, call 'Render' to create a BSON document for the index model.");

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexModel"/> class, passing the index
        /// model as a <see cref="BsonDocument"/>.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="CreateVectorSearchIndexModel{TDocument}"/> to build vector indexes without specifying
        /// the BSON directly.
        /// </remarks>
        /// <param name="name">The index name.</param>
        /// <param name="definition">The index definition.</param>
        public CreateSearchIndexModel(string name, BsonDocument definition)
            : this(name, null, definition)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexModel"/> class, passing the index
        /// model as a <see cref="BsonDocument"/>.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="CreateVectorSearchIndexModel{TDocument}"/> to build vector indexes without specifying
        /// the BSON directly.
        /// </remarks>
        /// <param name="name">The index name.</param>
        /// <param name="type">The index type.</param>
        /// <param name="definition">The index definition.</param>
        public CreateSearchIndexModel(string name, SearchIndexType? type, BsonDocument definition)
        {
            _name = name;
            _type = type;
            _definition = definition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSearchIndexModel"/> class.
        /// </summary>
        /// <param name="name">The index name.</param>
        /// <param name="type">The index type.</param>
        protected CreateSearchIndexModel(string name, SearchIndexType? type)
        {
            _name = name;
            _type = type;
        }
    }
}
