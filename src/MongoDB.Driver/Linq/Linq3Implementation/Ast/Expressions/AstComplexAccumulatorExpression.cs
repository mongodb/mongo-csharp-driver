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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstComplexAccumulatorExpression : AstAccumulatorExpression
    {
        private readonly AstComplexAccumulatorOperator _operator;
        private readonly IReadOnlyDictionary<string, AstExpression> _args;

        public AstComplexAccumulatorExpression(AstComplexAccumulatorOperator @operator, IReadOnlyDictionary<string, AstExpression> args)
        {
            _operator = @operator;
            _args = Ensure.IsNotNull(args, nameof(args));
        }

        public IReadOnlyDictionary<string, AstExpression> Args => _args;

        public override AstNodeType NodeType => AstNodeType.ComplexAccumulatorExpression;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitComplexAccumulatorExpression(this);
        }

        public override BsonValue Render()
        {
            var document = new BsonDocument();

            // Add all accumulator parameters
            foreach (var kvp in _args)
            {
                document[kvp.Key] = kvp.Value.Render();
            }

            return new BsonDocument(_operator.Render(), document);
        }

        public AstComplexAccumulatorExpression Update(IReadOnlyDictionary<string, AstExpression> args)
        {
            if (ReferenceEquals(args, _args))
            {
                return this;
            }

            return new AstComplexAccumulatorExpression(_operator, args);
        }
    }
}