using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Linq.Expressions;
using MongoDB.Linq.Translators;

namespace MongoDB.Linq
{
    internal class ExecutionBuilder : MongoExpressionVisitor
    {
        private Expression _provider;

        public Expression Build(Expression expression, Expression provider)
        {
            _provider = provider;
            return Visit(expression);
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            var queryObject = new MongoQueryObjectBuilder().Build(projection);
            queryObject.Projector = new ProjectionBuilder().Build(projection.Projector, queryObject.DocumentType, "document", queryObject.IsMapReduce);
            queryObject.Aggregator = (LambdaExpression)Visit(projection.Aggregator);

            Expression result = Expression.Call(
                _provider,
                "ExecuteQueryObject",
                Type.EmptyTypes,
                Expression.Constant(queryObject, typeof(MongoQueryObject)));

            if (queryObject.Aggregator != null)
                result = Expression.Convert(result, queryObject.Aggregator.Body.Type);
            else
                result = Expression.Convert(result, typeof(IEnumerable<>).MakeGenericType(queryObject.Projector.Body.Type));

            return result;
        }
    }
}