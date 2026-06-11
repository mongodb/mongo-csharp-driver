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
    internal sealed class AstPickExpression : AstExpression
    {
        #region static
        private static (AstVarExpression, AstExpression) EnsureAsAndSelectorAreValid(AstPickOperator @operator, AstVarExpression @as, AstExpression selector)
        {
            switch (@operator)
            {
                case AstPickOperator.FirstNArray:
                case AstPickOperator.LastNArray:
                case AstPickOperator.MaxNArray:
                case AstPickOperator.MinNArray:
                    Ensure.IsNull(@as, nameof(@as));
                    Ensure.IsNull(selector, nameof(selector));
                    break;

                case AstPickOperator.BottomPlaceholder:
                case AstPickOperator.BottomNPlaceholder:
                case AstPickOperator.FirstNPlaceholder:
                case AstPickOperator.LastNPlaceholder:
                case AstPickOperator.MaxNPlaceholder:
                case AstPickOperator.MinNPlaceholder:
                case AstPickOperator.TopPlaceholder:
                case AstPickOperator.TopNPlaceholder:
                    Ensure.IsNotNull(@as, nameof(@as));
                    Ensure.IsNotNull(selector, nameof(selector));
                    break;

                default:
                    throw new InvalidOperationException($"Invalid operator: {@operator}.");
            }

            return (@as, selector);
        }

        private static AstExpression EnsureNIsValid(AstPickOperator @operator, AstExpression n)
        {
            switch (@operator)
            {
                case AstPickOperator.BottomPlaceholder:
                case AstPickOperator.TopPlaceholder:
                    return Ensure.IsNull(n, nameof(n));

                case AstPickOperator.BottomNPlaceholder:
                case AstPickOperator.FirstNPlaceholder:
                case AstPickOperator.FirstNArray:
                case AstPickOperator.LastNPlaceholder:
                case AstPickOperator.LastNArray:
                case AstPickOperator.MaxNPlaceholder:
                case AstPickOperator.MaxNArray:
                case AstPickOperator.MinNPlaceholder:
                case AstPickOperator.MinNArray:
                case AstPickOperator.TopNPlaceholder:
                    return Ensure.IsNotNull(n, nameof(n));

                default:
                    throw new InvalidOperationException($"Invalid operator: {@operator}.");
            }
        }

        private static AstSortFields EnsureSortByIsValid(AstPickOperator @operator, AstSortFields sortBy)
        {
            switch (@operator)
            {
                case AstPickOperator.BottomPlaceholder:
                case AstPickOperator.BottomNPlaceholder:
                case AstPickOperator.TopPlaceholder:
                case AstPickOperator.TopNPlaceholder:
                    return Ensure.IsNotNull(sortBy, nameof(sortBy));

                case AstPickOperator.FirstNPlaceholder:
                case AstPickOperator.FirstNArray:
                case AstPickOperator.LastNPlaceholder:
                case AstPickOperator.LastNArray:
                case AstPickOperator.MaxNPlaceholder:
                case AstPickOperator.MaxNArray:
                case AstPickOperator.MinNPlaceholder:
                case AstPickOperator.MinNArray:
                    return Ensure.IsNull(sortBy, nameof(sortBy));

                default:
                    throw new InvalidOperationException($"Invalid operator: {@operator}.");
            }
        }
        #endregion

        private readonly AstVarExpression _as;
        private readonly AstExpression _n;
        private readonly AstPickOperator _operator;
        private readonly AstExpression _selector;
        private readonly AstSortFields _sortBy;
        private readonly AstExpression _source;

        public AstPickExpression(
            AstPickOperator @operator,
            AstExpression source,
            AstSortFields sortBy,
            AstVarExpression @as,
            AstExpression selector,
            AstExpression n)
        {
            _operator = @operator;
            _source = Ensure.IsNotNull(source, nameof(source));
            _sortBy = EnsureSortByIsValid(@operator, sortBy);
            (_as, _selector) = EnsureAsAndSelectorAreValid(@operator, @as, selector);
            _n = EnsureNIsValid(@operator, n);
        }

        public AstVarExpression As => _as;
        public AstExpression N => _n;
        public override AstNodeType NodeType => AstNodeType.PickExpression;
        public AstPickOperator Operator => _operator;
        public AstExpression Selector => _selector;
        public AstSortFields SortBy => _sortBy;
        public AstExpression Source => _source;

        public override AstNode Accept(AstNodeVisitor visitor)
        {
            return visitor.VisitPickExpression(this);
        }

        public override BsonValue Render()
        {
            switch (_operator)
            {
                case AstPickOperator.BottomPlaceholder:
                case AstPickOperator.BottomNPlaceholder:
                case AstPickOperator.TopPlaceholder:
                case AstPickOperator.TopNPlaceholder:
                    return new BsonDocument
                    {
                        { _operator.Render(), new BsonDocument
                            {
                                { "source", _source.Render() },
                                { "as", _as.Render() },
                                { "sortBy", new BsonDocument(_sortBy.Select(f => f.RenderAsElement())) },
                                { "output", _selector.Render() },
                                { "n", _n?.Render(), _n != null }
                            }
                        }
                    };

                case AstPickOperator.FirstNPlaceholder:
                case AstPickOperator.LastNPlaceholder:
                case AstPickOperator.MaxNPlaceholder:
                case AstPickOperator.MinNPlaceholder:
                    return new BsonDocument
                    {
                        { _operator.Render(), new BsonDocument
                            {
                                { "source", _source.Render() },
                                { "as", _as.Render() },
                                { "input", _selector.Render() },
                                { "n", _n.Render() }
                            }
                        }
                    };

                case AstPickOperator.FirstNArray:
                case AstPickOperator.LastNArray:
                case AstPickOperator.MaxNArray:
                case AstPickOperator.MinNArray:
                    return new BsonDocument
                    {
                        { _operator.Render(), new BsonDocument
                            {
                                { "input", _source.Render() },
                                { "n", _n.Render() }
                            }
                        }
                    };

                default:
                    throw new InvalidOperationException($"Invalid operator: {_operator}.");
            }
        }

        public AstPickExpression Update(
            AstPickOperator @operator,
            AstExpression source,
            AstVarExpression @as,
            AstSortFields sortBy,
            AstExpression selector,
            AstExpression n)
        {
            if (@operator == _operator && source == _source && @as == _as && sortBy == _sortBy && selector == _selector && n == _n)
            {
                return this;
            }

            return new AstPickExpression(@operator, source, sortBy, @as, selector, n);
        }
    }
}
