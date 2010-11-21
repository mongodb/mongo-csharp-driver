using System;
using System.Linq.Expressions;

namespace MongoDB.Linq.Expressions
{
    internal class OrderExpression : MongoExpression
    {
        public Expression Expression { get; private set; }

        public OrderType OrderType { get; private set; }

        public OrderExpression(OrderType orderType, Expression expression)
            : base(MongoExpressionType.Order, expression.Type)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            Expression = expression;
            OrderType = orderType;
        }
    }
}