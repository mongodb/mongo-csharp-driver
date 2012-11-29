/* Copyright 2010-2012 10gen Inc.
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
using System.Text;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A translator from LINQ expression queries to Mongo queries.
    /// </summary>
    public static class MongoQueryTranslator
    {
        // public static methods
        /// <summary>
        /// Translate a MongoDB LINQ query.
        /// </summary>
        /// <param name="query">The MongoDB LINQ query.</param>
        /// <returns>A TranslatedQuery.</returns>
        public static TranslatedQuery Translate(IQueryable query)
        {
            return Translate((MongoQueryProvider)query.Provider, query.Expression);
        }

        /// <summary>
        /// Translate a MongoDB LINQ query.
        /// </summary>
        /// <param name="provider">The MongoDB query provider.</param>
        /// <param name="expression">The LINQ query expression.</param>
        /// <returns>A TranslatedQuery.</returns>
        public static TranslatedQuery Translate(MongoQueryProvider provider, Expression expression)
        {
            expression = PartialEvaluator.Evaluate(expression, provider);
            expression = ExpressionNormalizer.Normalize(expression);
            // assume for now it's a SelectQuery
            var documentType = GetDocumentType(expression);
            var selectQuery = new SelectQuery(provider.Collection, documentType);
            selectQuery.Translate(expression);
            return selectQuery;
        }

        // private static methods
        private static Type GetDocumentType(Expression expression)
        {
            // look for the innermost nested constant of type MongoQueryable<T> and return typeof(T)
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
            {
                var constantType = constantExpression.Type;
                if (constantType.IsGenericType)
                {
                    var genericTypeDefinition = constantType.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(MongoQueryable<>))
                    {
                        return constantType.GetGenericArguments()[0];
                    }
                }
            }

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression != null && methodCallExpression.Arguments.Count != 0)
            {
                return GetDocumentType(methodCallExpression.Arguments[0]);
            }

            var message = string.Format("Unable to find document type of expression: {0}.", ExpressionFormatter.ToString(expression));
            throw new ArgumentOutOfRangeException("expression", message);
        }
    }
}
