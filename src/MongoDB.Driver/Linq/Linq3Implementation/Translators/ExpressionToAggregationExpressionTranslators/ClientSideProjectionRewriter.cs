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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal class ClientSideProjectionRewriter: ExpressionVisitor
    {
        #region static
        public static (TranslatedExpression[], LambdaExpression) RewriteProjection(TranslationContext context, LambdaExpression projectionLambda, IBsonSerializer sourceSerializer)
        {
            var rootParameter = projectionLambda.Parameters.Single();
            var rootSymbol = context.CreateRootSymbol(rootParameter, sourceSerializer);
            context = context.WithSymbol(rootSymbol);

            var snippetsParameter = Expression.Parameter(typeof(object[]), "snippets");
            var projectionRewriter = new ClientSideProjectionRewriter(context, snippetsParameter);
            var rewrittenBody = projectionRewriter.Visit(projectionLambda.Body);
            var rewrittenLambda = Expression.Lambda(rewrittenBody, snippetsParameter);
            var snippetsArray = projectionRewriter.Snippets.ToArray();

            return (snippetsArray, rewrittenLambda);
        }
        #endregion

        private readonly TranslationContext _context;
        private readonly List<TranslatedExpression> _snippets = new();
        private readonly ParameterExpression _snippetsParameter;

        private ClientSideProjectionRewriter(TranslationContext context, ParameterExpression snippetsParameter)
        {
            _context = context;
            _snippetsParameter = snippetsParameter;
        }

        private List<TranslatedExpression> Snippets => _snippets;

        public override Expression Visit(Expression node)
        {
            if (node == null)
            {
                return null;
            }

            if (node.NodeType == ExpressionType.Constant)
            {
                return node; // don't make snippets for constants
            }

            TranslatedExpression snippet;
            try
            {
                snippet = ExpressionToAggregationExpressionTranslator.Translate(_context, node);
            }
            catch
            {
                return base.Visit(node); // try to find smaller snippets below this node
            }

            var snippetIndex = _snippets.Count;
            _snippets.Add(snippet);

            var snippetReference = // (T)snippets[i]
                Expression.Convert(
                    Expression.ArrayIndex(_snippetsParameter, Expression.Constant(snippetIndex)),
                    snippet.Expression.Type);

            return snippetReference;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // don't split OrderBy/ThenBy across the client/server boundary
            if (node.Method.IsOneOf(EnumerableOrQueryableMethod.ThenByOverloads))
            {
                return VisitThenBy(node);
            }

            return base.VisitMethodCall(node);
        }

        private Expression VisitThenBy(MethodCallExpression node)
        {
            var arguments = node.Arguments;
            var sourceExpression = arguments[0];
            var keySelectorExpression = arguments[1];

            if (sourceExpression is MethodCallExpression sourceMethodCallExpression)
            {
                var sourceMethod = sourceMethodCallExpression.Method;

                if (sourceMethod.IsOneOf(EnumerableOrQueryableMethod.ThenByOverloads))
                {
                    var rewrittenSourceExpression = VisitThenBy(sourceMethodCallExpression);
                    return node.Update(node.Object, [rewrittenSourceExpression, keySelectorExpression]);
                }

                if (sourceMethod.IsOneOf(EnumerableOrQueryableMethod.OrderByOverloads))
                {
                    var rewrittenSourceExpression = VisitOrderBy(sourceMethodCallExpression);
                    return node.Update(node.Object, [rewrittenSourceExpression, keySelectorExpression]);
                }
            }

            throw new ArgumentException("ThenBy or ThenByDescending not preceded by OrderBy or OrderByDescending.", nameof(node));
        }

        private Expression VisitOrderBy(MethodCallExpression node)
        {
            var arguments = node.Arguments;
            var sourceExpression = arguments[0];
            var keySelectorExpression = arguments[1];
            var rewrittenSourceExpression = Visit(sourceExpression);
            return node.Update(node.Object, [rewrittenSourceExpression, keySelectorExpression]);
        }
    }
}
