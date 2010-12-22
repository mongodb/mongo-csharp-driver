#region

using System;
using System.Linq.Expressions;
using MongoDB.Driver.Builders.TypedBuilder;

#endregion

namespace MongoDB.Driver.Builders
{
    public class TypedQueryBuilder
    {
        public static UpdateBuilder Update<T>(Expression<Func<T>> updateExpression)
        {
            if (updateExpression.Body.NodeType != ExpressionType.MemberInit)
            {
                throw new InvalidOperationException("Expected MemberInit node type");
            }
            var updateExpressionVisitor = new UpdateExpressionVisitor();
            updateExpressionVisitor.Visit(ConstantEvaluator.Eval(updateExpression));
            return updateExpressionVisitor.UpdateBuilder;
        }

        public static QueryComplete Where<T>(Expression<Func<T, bool>> predicate)
        {
            var visitor = new WhereExpressionVisitor();
            visitor.Visit(ConstantEvaluator.Eval(predicate));

            return visitor.SearchQuery;
        }
    }
}