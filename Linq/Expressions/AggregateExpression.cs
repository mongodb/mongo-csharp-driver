using System;
using System.Linq.Expressions;

namespace MongoDB.Linq.Expressions
{
    internal class AggregateExpression : MongoExpression
    {
        public AggregateType AggregateType { get; private set; }

        public Expression Argument { get; private set; }

        public bool Distinct { get; private set; }

        public AggregateExpression(Type type, AggregateType aggregateType, Expression argument, bool distinct)
            : base(MongoExpressionType.Aggregate, type)
        {
            AggregateType = aggregateType;
            Argument = argument;
            Distinct = distinct;
        }
    }
}
