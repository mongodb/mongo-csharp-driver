using System.Linq.Expressions;

namespace MongoDB.Linq.Expressions
{
    internal class AggregateSubqueryExpression : MongoExpression
    {
        public Expression AggregateInGroupSelect { get; private set; }

        public ScalarExpression AggregateAsSubquery { get; private set; }

        public Alias GroupByAlias { get; private set; }

        public AggregateSubqueryExpression(Alias groupByAlias, Expression aggregateInGroupSelect, ScalarExpression aggregateAsSubquery)
            : base(MongoExpressionType.AggregateSubquery, aggregateAsSubquery.Type)
        {
            GroupByAlias = groupByAlias;
            AggregateInGroupSelect = aggregateInGroupSelect;
            AggregateAsSubquery = aggregateAsSubquery;
        }
    }
}