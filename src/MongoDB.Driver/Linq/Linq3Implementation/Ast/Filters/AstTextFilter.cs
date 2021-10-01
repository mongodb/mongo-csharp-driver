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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters
{
    internal sealed class AstTextFilter : AstFilter
    {
        private readonly bool? _caseSensitive;
        private readonly bool? _diacriticSensitive;
        private readonly string _language;
        private readonly string _search;

        public AstTextFilter(
            string search,
            string language = default,
            bool? caseSensitive = default,
            bool? diacriticSensitive = default)
        {
            _search = Ensure.IsNotNull(search, nameof(search));
            _language = language; // optional
            _caseSensitive = caseSensitive; // optional
            _diacriticSensitive = diacriticSensitive; // optional
        }

        public bool? CaseSensitive => _caseSensitive;
        public bool? DiacriticSensitive => _diacriticSensitive;
        public string Language => _language;
        public override AstNodeType NodeType => AstNodeType.TextFilter;
        public string Search => _search;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitTextFilter(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$text", new BsonDocument
                    {
                        { "$search", _search  },
                        { "$language", _language, _language != null },
                        { "$caseSensitive", () => _caseSensitive.Value, _caseSensitive != default },
                        { "$diacriticSensitive", () => _diacriticSensitive.Value, _diacriticSensitive != default }
                    }
                }
            };
        }
    }
}
