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
    internal sealed class CreateSearchIndexRequest
    {
        public string Name { get; }
        public SearchIndexType? Type { get; }
        public BsonDocument Definition { get; }

        public CreateSearchIndexRequest(string name, SearchIndexType? type, BsonDocument definition)
        {
            Name = name;
            Type = type;
            Definition = Ensure.IsNotNull(definition, nameof(definition));
        }

        // methods
        internal BsonDocument CreateIndexDocument() =>
            new()
            {
                { "name", Name, Name != null },
                { "type", Type == SearchIndexType.VectorSearch ? "vectorSearch" : "search", Type != null },
                { "definition", Definition }
            };
    }
}
