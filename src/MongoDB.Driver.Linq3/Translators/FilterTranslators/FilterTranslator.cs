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

using System.Linq.Expressions;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.FilterTranslators
{
    public class FilterTranslator
    {
        // private fields
        private readonly TranslationContext _context;

        // constructors
        public FilterTranslator(TranslationContext context)
        {
            _context = Throw.IfNull(context, nameof(context));
        }

        // public methods
        public AstFilter Translate(Expression expression)
        {
            AstFilter filter;
            switch (expression.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    if (TryTranslateAnd((BinaryExpression)expression, out filter))
                    {
                        return filter;
                    }
                    break;
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    if (TryTranslateComparison((BinaryExpression)expression, out filter))
                    {
                        return filter;
                    }
                    break;

                case ExpressionType.Not:
                    if (TryTranslateNot((UnaryExpression)expression, out filter))
                    {
                        return filter;
                    }
                    break;

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    if (TryTranslateOr((BinaryExpression)expression, out filter))
                    {
                        return filter;
                    }
                    break;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private methods
        private bool TryTranslateAnd(BinaryExpression expression, out AstFilter filter)
        {
            filter = null;

            var left = expression.Left;
            var right = expression.Right;

            if (left.Type == typeof(bool) && right.Type == typeof(bool))
            {
                var clause1 = Translate(left);
                var clause2 = Translate(right);
                //filter = new BsonDocument("$and", new BsonArray { clause1, clause2 });
                filter = new AstAndFilter(clause1, clause2);
                return true;
            }

            return false;
        }

        private bool TryTranslateComparison(BinaryExpression expression, out AstFilter filter)
        {
            filter = null;

            AstComparisonFilterOperator comparisonOperator;
            switch (expression.NodeType)
            {
                case ExpressionType.Equal: comparisonOperator = AstComparisonFilterOperator.Eq; break;
                case ExpressionType.GreaterThan: comparisonOperator = AstComparisonFilterOperator.Gt; break;
                case ExpressionType.GreaterThanOrEqual: comparisonOperator = AstComparisonFilterOperator.Gte; break;
                case ExpressionType.LessThan: comparisonOperator = AstComparisonFilterOperator.Lt; break;
                case ExpressionType.LessThanOrEqual: comparisonOperator = AstComparisonFilterOperator.Lte; break;
                case ExpressionType.NotEqual: comparisonOperator = AstComparisonFilterOperator.Ne; break;
                default: return false;
            }

            var left = expression.Left;
            var right = expression.Right;

            if (left.NodeType == ExpressionType.Constant && right.NodeType != ExpressionType.Constant)
            {
                var temp = left;
                left = right;
                right = temp;

                switch (comparisonOperator)
                {
                    case AstComparisonFilterOperator.Gte: comparisonOperator = AstComparisonFilterOperator.Lt; break;
                    case AstComparisonFilterOperator.Gt: comparisonOperator  = AstComparisonFilterOperator.Lte; break;
                    case AstComparisonFilterOperator.Lte: comparisonOperator = AstComparisonFilterOperator.Gt; break;
                    case AstComparisonFilterOperator.Lt: comparisonOperator  = AstComparisonFilterOperator.Gte; break;
                }
            }

            var fieldResolver = new FieldResolver(_context.SymbolTable);
            if (fieldResolver.TryResolveField(left, out ResolvedField resolvedField))
            {
                if (right is ConstantExpression constantExpression)
                {
                    var value = constantExpression.Value;
                    var serializedValue = SerializationHelper.SerializeValue(resolvedField.Serializer, value);

                    //filter = new BsonDocument(resolvedField.DottedFieldName, new BsonDocument(comparisonOperator, serializedValue));
                    var field = new AstFieldExpression(resolvedField.DottedFieldName);
                    filter = new AstComparisonFilter(comparisonOperator, field, serializedValue);
                    return true;
                }
            }

            return false;
        }

        private bool TryTranslateNot(UnaryExpression expression, out AstFilter filter)
        {
            var clause = Translate(expression.Operand);
            //filter = new BsonDocument("$nor", new BsonArray { clause });
            filter = new AstNotFilter(clause);
            return true;
        }

        private bool TryTranslateOr(BinaryExpression expression, out AstFilter filter)
        {
            filter = null;

            var left = expression.Left;
            var right = expression.Right;
            if (left.Type == typeof(bool) && right.Type == typeof(bool))
            {
                var clause1 = Translate(left);
                var clause2 = Translate(right);
                //filter = new BsonDocument("$or", new BsonArray { clause1, clause2 });
                filter = new AstOrFilter(clause1, clause2);
                return true;
            }

            return false;
        }
    }
}
