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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for creating a clustered index.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    public class ClusteredIndexOptions<TDocument>
    {
        private IndexKeysDefinition<TDocument> _key;
        private string _name;
        private bool _unique;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusteredIndexOptions{TDocument}"/> class.
        /// </summary>
        public ClusteredIndexOptions()
        {
            _key = new BsonDocument { { "_id", 1 } };
            _unique = true;
        }

        /// <summary>
        /// Gets or sets the index key, which must currently be {_id: 1}.
        /// </summary>
        public IndexKeysDefinition<TDocument> Key
        {
            get => _key;
            set => _key = value;
        }

        /// <summary>
        /// Gets or sets the index name.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// Gets or sets whether the index entries must be unique, which currently must be true.
        /// </summary>
        public bool Unique
        {
            get => _unique;
            set => _unique = value;
        }

        internal BsonDocument Render(IBsonSerializer<TDocument> documentSerializer, IBsonSerializerRegistry serializerRegistry, ExpressionTranslationOptions translationOptions)
        {
            return new BsonDocument {
                { "key", _key.Render(new(documentSerializer, serializerRegistry, translationOptions: translationOptions)) },
                { "unique", _unique },
                { "name", _name, _name != null }
            };
        }
    }
}
