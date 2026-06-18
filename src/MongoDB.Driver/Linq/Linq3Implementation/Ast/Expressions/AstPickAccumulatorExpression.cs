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
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;

namespace MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions
{
    internal sealed class AstPickAccumulatorExpression : AstAccumulatorExpression
    {
        private readonly AstExpression _n;
        private readonly AstPickAccumulatorOperator _operator;
        private readonly AstExpression _selector;
        private readonly AstSortFields _sortBy;

        public AstPickAccumulatorExpression(
            AstPickAccumulatorOperator @operator,
            AstSortFields sortBy,
            AstExpression selector,
            AstExpression n)
        {
            _operator = @operator;
            _sortBy = @operator.EnsureSortByIsValid(sortBy);
            _selector = Ensure.IsNotNull(selector, nameof(selector));
            _n = @operator.EnsureNIsValid(n);
        }

        public AstExpression N => _n;
        public override AstNodeType NodeType => AstNodeType.PickAccumulatorExpression;
        public AstPickAccumulatorOperator Operator => _operator;
        public AstExpression Selector => _selector;
        public AstSortFields SortBy => _sortBy;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitPickAccumulatorExpression(this);
        }

        public override BsonValue Render()
        {
            switch (_operator)
            {
                case AstPickAccumulatorOperator.Bottom:
                case AstPickAccumulatorOperator.BottomN:
                case AstPickAccumulatorOperator.Top:
                case AstPickAccumulatorOperator.TopN:
                    return new BsonDocument
                    {
                        { _operator.Render(), new BsonDocument
                            {
                                { "sortBy", new BsonDocument(_sortBy.Select(f => f.RenderAsElement())) },
                                { "output", _selector.Render() },
                                { "n", _n?.Render(), _n != null }
                            }
                        }
                    };

                case AstPickAccumulatorOperator.FirstN:
                case AstPickAccumulatorOperator.LastN:
                case AstPickAccumulatorOperator.MaxN:
                case AstPickAccumulatorOperator.MinN:
                    return new BsonDocument
                    {
                        { _operator.Render(), new BsonDocument
                            {
                                { "input", _selector.Render() },
                                { "n", _n?.Render(), _n != null }
                            }
                        }
                    };

                default:
                    throw new InvalidOperationException($"Invalid operator: {_operator}.");
            }
        }

        public AstPickAccumulatorExpression Update(
            AstPickAccumulatorOperator @operator,
            AstSortFields sortBy,
            AstExpression selector,
            AstExpression n)
        {
            if (@operator == _operator && sortBy == _sortBy && selector == _selector && n == _n)
            {
                return this;
            }

            return new AstPickAccumulatorExpression(@operator, sortBy, selector, n);
        }
    }
}
