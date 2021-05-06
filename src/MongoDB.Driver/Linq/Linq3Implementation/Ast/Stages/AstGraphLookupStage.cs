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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstGraphLookupStage : AstStage
    {
        private readonly string _as;
        private readonly string _connectFromField;
        private readonly string _connectToField;
        private readonly string _depthField;
        private readonly string _from;
        private readonly int? _maxDepth;
        private readonly AstFilter _restrictSearchWithMatch;
        private readonly AstExpression _startWith;

        public AstGraphLookupStage(
            string from,
            AstExpression startWith,
            string connectFromField,
            string connectToField,
            string @as,
            int? maxDepth = default,
            string depthField = default,
            AstFilter restrictSearchWithMatch = default)
        {
            _from = Ensure.IsNotNull(from, nameof(from));
            _startWith = Ensure.IsNotNull(startWith, nameof(startWith));
            _connectFromField = Ensure.IsNotNull(connectFromField, nameof(connectFromField));
            _connectToField = Ensure.IsNotNull(connectToField, nameof(connectToField));
            _as = Ensure.IsNotNull(@as, nameof(@as));
            _maxDepth = maxDepth;
            _depthField = depthField;
            _restrictSearchWithMatch = restrictSearchWithMatch;
        }

        public string As => _as;
        public string From => _from;
        public string ConnectFromField => _connectFromField;
        public string ConnectToField => _connectToField;
        public string DepthField => _depthField;
        public int? MaxDepth => _maxDepth;
        public override AstNodeType NodeType => AstNodeType.GraphLookupStage;
        public AstFilter RestrictSearchWithMatch => _restrictSearchWithMatch;
        public AstExpression StartWith => _startWith;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$graphLookup", new BsonDocument
                    {
                        { "from", _from },
                        { "startWith", _startWith.Render() },
                        { "connectFromField", _connectFromField },
                        { "connectToField", _connectToField },
                        { "as", _as },
                        { "maxDepth", () => _maxDepth.Value, _maxDepth.HasValue },
                        { "depthField", _depthField, _depthField != null },
                        { "restrictSearchWithMatch", () => _restrictSearchWithMatch.Render(), _restrictSearchWithMatch != null }
                    }
                }
            };
        }
    }
}
