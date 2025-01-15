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
    internal class ClientSideProjectionSnippetsTranslator : ExpressionVisitor
    {
        #region static

        private readonly static MethodInfo[] __orderByMethods =
        [
            EnumerableMethod.OrderBy,
            EnumerableMethod.OrderByDescending,
            QueryableMethod.OrderBy,
            QueryableMethod.OrderByDescending
        ];

        private readonly static MethodInfo[] __thenByMethods =
        [
            EnumerableMethod.ThenBy,
            EnumerableMethod.ThenByDescending,
            QueryableMethod.ThenBy,
            QueryableMethod.ThenByDescending
        ];

        public static AggregationExpression[] TranslateSnippets(TranslationContext context, LambdaExpression selectorLambda, IBsonSerializer sourceSerializer)
        {
            var rootParameter = selectorLambda.Parameters.Single();
            var rootSymbol = context.CreateRootSymbol(rootParameter, sourceSerializer);
            context = context.WithSymbol(rootSymbol);

            var snippetTranslator = new ClientSideProjectionSnippetsTranslator(context, rootParameter);
            snippetTranslator.Visit(selectorLambda.Body);

            return snippetTranslator.Snippets.ToArray();
        }

        #endregion

        private readonly TranslationContext _context;
        private readonly ParameterExpression _rootParameter;
        private readonly List<AggregationExpression> _snippets = new();

        private ClientSideProjectionSnippetsTranslator(TranslationContext context, ParameterExpression rootParameter)
        {
            _context = context;
            _rootParameter = rootParameter;
        }

        private List<AggregationExpression> Snippets => _snippets;

        public override Expression Visit(Expression node)
        {
            if (ExpressionIsReferencedVisitor.IsReferenced(node, _rootParameter))
            {
                try
                {
                    var snippet = ExpressionToAggregationExpressionTranslator.Translate(_context, node);
                    _snippets.Add(snippet);
                    return node;
                }
                catch
                {
                    // don't split OrderBy/ThenBy between client and server
                    if (node is MethodCallExpression methodCallExpression &&
                        methodCallExpression.Method.IsOneOf(__thenByMethods))
                    {
                        var orderBySource = FindOrderBySource(node);
                        Visit(orderBySource); // resume visiting at orderBySource
                        return node; // suppress any further visiting below this node
                    }

                    // ignore exceptions and fall through
                }
            }

            return base.Visit(node);

            static Expression FindOrderBySource(Expression node)
            {
                if (node is MethodCallExpression methodCallExpression)
                {
                    if (methodCallExpression.Method.IsOneOf(__thenByMethods))
                    {
                        return FindOrderBySource(methodCallExpression.Arguments[0]);
                    }

                    if (methodCallExpression.Method.IsOneOf(__orderByMethods))
                    {
                        return methodCallExpression.Arguments[0];
                    }
                }

                throw new ArgumentException($"Node type {node.NodeType} is not a MethodCallExpression.");
            }
        }
    }
}
