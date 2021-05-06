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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstDateToPartsExpression : AstExpression
    {
        private readonly AstExpression _expression;
        private readonly AstExpression _iso8601;
        private readonly AstExpression _timezone;

        public AstDateToPartsExpression(
            AstExpression expression,
            AstExpression timezone = null,
            AstExpression iso8601 = null)
        {
            _expression = Ensure.IsNotNull(expression, nameof(expression));
            _timezone = timezone;
            _iso8601 = iso8601;
        }

        public AstExpression Expression => _expression;
        public AstExpression Iso8601 => _iso8601;
        public override AstNodeType NodeType => AstNodeType.DateToPartsExpression;
        public AstExpression Timezone => _timezone;

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$dateToParts", new BsonDocument
                    {
                        { "date", _expression.Render() },
                        { "timezone", () => _timezone.Render(), _timezone != null },
                        { "iso8601", () => _iso8601.Render(), _iso8601 != null }
                    }
                }
            };
        }
    }
}
