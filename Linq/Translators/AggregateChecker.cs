using System.Linq.Expressions;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class AggregateChecker : MongoExpressionVisitor
    {
        private bool _hasAggregate;

        public bool HasAggregates(Expression expression)
        {
            _hasAggregate = false;
            Visit(expression);
            return _hasAggregate;
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            _hasAggregate = true;
            return aggregate;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            Visit(select.Where);
            VisitOrderBy(select.OrderBy);
            VisitFieldDeclarationList(select.Fields);
            return select;
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            return subquery;
        }
    }
}
