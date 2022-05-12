﻿/* Copyright 2010-present MongoDB Inc.
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
    internal sealed class AstFilterExpression : AstExpression
    {
        private readonly string _as;
        private readonly AstExpression _cond;
        private readonly AstExpression _input;
        private readonly AstExpression _limit;

        public AstFilterExpression(
            AstExpression input,
            AstExpression cond,
            string @as = null,
            AstExpression limit = null)
        {
            _input = Ensure.IsNotNull(input, nameof(input));
            _cond = Ensure.IsNotNull(cond, nameof(cond));
            _as = @as;
            _limit = limit;
        }

        public string As => _as;
        public new AstExpression Cond => _cond;
        public AstExpression Input => _input;
        public AstExpression Limit => _limit;
        public override AstNodeType NodeType => AstNodeType.FilterExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitFilterExpression(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument
            {
                { "$filter", new BsonDocument
                    {
                        { "input", _input.Render() },
                        { "as", _as, _as != null },
                        { "cond", _cond.Render() },
                        { "limit", () => _limit.Render(), _limit != null }
                    }
                }
            };
        }

        public AstFilterExpression Update(
            AstExpression input,
            AstExpression cond,
            AstExpression limit)
        {
            if (input == _input && cond == _cond)
            {
                return this;
            }

            return new AstFilterExpression(input, cond, _as, limit);
        }
    }
}
