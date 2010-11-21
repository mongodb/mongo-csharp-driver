using System.Collections.Generic;
using System.Linq;
using MongoDB.Linq.Expressions;
using System.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class AggregateRewriter : MongoExpressionVisitor
    {
        ILookup<Alias, AggregateSubqueryExpression> _lookup;
        readonly Dictionary<AggregateSubqueryExpression, Expression> _map;

        public AggregateRewriter()
        {
            _map = new Dictionary<AggregateSubqueryExpression, Expression>();
        }

        public Expression Rewrite(Expression expression)
        {
            _lookup = new AggregateGatherer().Gather(expression).ToLookup(x => x.GroupByAlias);
            return Visit(expression);
        }

        protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
        {
            Expression mapped;
            if (_map.TryGetValue(aggregate, out mapped))
                return mapped;

            return Visit(aggregate.AggregateAsSubquery);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression)base.VisitSelect(select);
            if (_lookup.Contains(select.Alias))
            {
                var fields = new List<FieldDeclaration>(select.Fields);
                foreach (var ae in _lookup[select.Alias])
                {
                    var name = "_$agg" + fields.Count;
                    var field = new FieldDeclaration(name, ae.AggregateInGroupSelect);
                    if (_map.ContainsKey(ae))
                        continue;
                    _map.Add(ae, new FieldExpression(ae.AggregateInGroupSelect, ae.GroupByAlias, name));
                    fields.Add(field);
                }
                return new SelectExpression(select.Alias, fields, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
            }
            return select;
        }

        private class AggregateGatherer : MongoExpressionVisitor
        {
            private readonly List<AggregateSubqueryExpression> _aggregates;

            public AggregateGatherer()
            {
                _aggregates = new List<AggregateSubqueryExpression>();
            }

            public IEnumerable<AggregateSubqueryExpression> Gather(Expression expression)
            {
                Visit(expression);
                return _aggregates;
            }

            protected override Expression VisitAggregateSubquery(AggregateSubqueryExpression aggregate)
            {
                _aggregates.Add(aggregate);
                return base.VisitAggregateSubquery(aggregate);
            }
        }
    }
}