/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.MethodCallBinders
{
    internal class OrderByBinder : IMethodCallBinder
    {
        public Expression Bind(ProjectionExpression projection, ProjectionBindingContext context, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            var source = projection.Source;
            var sortClauses = GatherPreviousSortClauses(projection.Source, out source);

            var lambda = ExtensionExpressionVisitor.GetLambda(arguments.Single());
            var binder = new AccumulatorBinder(context.GroupMap, context.SerializerRegistry);
            binder.RegisterParameterReplacement(lambda.Parameters[0], projection.Projector);

            var direction = GetDirection(node.Method.Name);
            var ordering = binder.Bind(lambda.Body);

            sortClauses.Add(new SortClause(ordering, direction));

            return new ProjectionExpression(
                new OrderByExpression(
                    source,
                    sortClauses),
                projection.Projector,
                projection.Aggregator);
        }

        private List<SortClause> GatherPreviousSortClauses(Expression node, out Expression previous)
        {
            var clauses = new List<SortClause>();
            previous = node;
            var current = previous as OrderByExpression;
            while (current != null)
            {
                previous = current.Source;
                clauses.AddRange(current.Clauses);
                current = previous as OrderByExpression;
            }

            return clauses;
        }

        private SortDirection GetDirection(string name)
        {
            switch (name)
            {
                case "OrderBy":
                case "ThenBy":
                    return SortDirection.Ascending;
                case "OrderByDescending":
                case "ThenByDescending":
                    return SortDirection.Descending;
            }

            throw new MongoInternalException("Unknown sort direction.");
        }

    }
}
