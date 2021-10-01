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

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstRegexExpression : AstExpression
    {
        private readonly AstExpression _input;
        private readonly AstRegexOperator _operator;
        private readonly AstExpression _options;
        private readonly AstExpression _regex;

        public AstRegexExpression(
            AstRegexOperator @operator,
            AstExpression input,
            AstExpression regex,
            AstExpression options = null)
        {
            _operator = @operator;
            _input = Ensure.IsNotNull(input, nameof(input));
            _regex = Ensure.IsNotNull(regex, nameof(regex));
            _options = options;
        }

        public AstExpression Input => _input;
        public override AstNodeType NodeType => AstNodeType.RegexExpression;
        public AstRegexOperator Operator => _operator;
        public AstExpression Options => _options;
        public AstExpression Regex => _regex;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitRegexExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { _operator.Render(), new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "regex", _regex.Render() },
                        { "options", () => _options.Render(), _options != null }
                    }
                }
            };
        }

        public AstRegexExpression Update(
            AstExpression input,
            AstExpression regex,
            AstExpression options)
        {
            if (input == _input && regex == _regex && options == _options)
            {
                return this;
            }

            return new AstRegexExpression(_operator, input, regex, options);
        }
    }
}
