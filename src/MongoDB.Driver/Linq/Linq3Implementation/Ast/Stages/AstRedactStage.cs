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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages
{
    internal sealed class AstRedactStage : AstStage
    {
        private readonly AstExpression _expression;

        public AstRedactStage(AstExpression expression)
        {
            _expression = Ensure.IsNotNull(expression, nameof(expression));
        }

        public AstExpression Expression => _expression;
        public override AstNodeType NodeType => AstNodeType.RedactStage;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitRedactStage(this);
        }

        public override BsonValue Render()
        {
            return new BsonDocument("$redact", _expression.Render());
        }

        public AstRedactStage Update(AstExpression expression)
        {
            if (expression == _expression)
            {
                return this;
            }

            return new AstRedactStage(expression);
        }
    }
}
