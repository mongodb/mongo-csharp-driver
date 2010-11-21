using System.Linq;
using System.Linq.Expressions;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class RedundantSubqueryRemover : MongoExpressionVisitor
    {
        private bool _isTopLevel;

        public Expression Remove(Expression expression)
        {
            _isTopLevel = true;
            return Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool wasTopLevel = _isTopLevel;
            _isTopLevel = false;

            select = (SelectExpression)base.VisitSelect(select);

            while (CanMergeWithFrom(select, wasTopLevel))
            {
                var fromSelect = (SelectExpression)select.From;

                select = (SelectExpression)new SubqueryRemover().Remove(select, new[] { fromSelect });
                   
                var where = select.Where;
                if(fromSelect.Where != null)
                {
                    if (where != null)
                        where = Expression.And(fromSelect.Where, where);
                    else
                        where = fromSelect.Where;
                }

                var groupBy = select.GroupBy ?? fromSelect.GroupBy;
                var orderBy = select.OrderBy != null && select.OrderBy.Count > 0 ? select.OrderBy : fromSelect.OrderBy;
                var skip = select.Skip ?? fromSelect.Skip;
                var take = select.Take ?? fromSelect.Take;
                bool distinct = select.IsDistinct | fromSelect.IsDistinct;
                var fields = select.Fields.Count > 0 ? select.Fields : fromSelect.Fields;

                if (where != select.Where
                    || orderBy != select.OrderBy
                    || groupBy != select.GroupBy
                    || distinct != select.IsDistinct
                    || skip != select.Skip
                    || take != select.Take
                    || fields != select.Fields)
                {
                    select = new SelectExpression(select.Alias, fields, select.From, where, orderBy, groupBy, distinct, skip, take);
                }
            }

            return select;
        }
        
        private static bool CanMergeWithFrom(SelectExpression select, bool isTopLevel)
        {
            var fromSelect = select.From as SelectExpression;
            if (fromSelect == null)
                return false;

            var fromIsSimpleProjection = IsSimpleProjection(fromSelect);
            var fromIsNameMapProjection = IsNameMapProjection(fromSelect);
            if (!fromIsSimpleProjection && !fromIsNameMapProjection)
                return false;

            var selectIsNameMapProjection = IsNameMapProjection(select);
            var selectHasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
            var selectHasGroupBy = select.GroupBy != null;
            var selectHasAggregates = new AggregateChecker().HasAggregates(select);
            var fromHasOrderBy = fromSelect.OrderBy != null && fromSelect.OrderBy.Count > 0;
            var fromHasGroupBy = fromSelect.GroupBy != null;

            if (selectHasOrderBy && fromHasOrderBy)
                return false;

            if (selectHasGroupBy && fromHasGroupBy)
                return false;

            if(fromHasOrderBy && (selectHasGroupBy || selectHasAggregates || select.IsDistinct))
                return false;

            if(fromHasGroupBy && select.Where != null)
                return false;

            if(fromSelect.Take != null && (select.Take != null || select.Skip != null || select.IsDistinct || selectHasAggregates || selectHasGroupBy))
                return false;

            if(fromSelect.Skip != null && (select.Skip != null || select.IsDistinct || selectHasAggregates || selectHasGroupBy))
                return false;

            if (fromSelect.IsDistinct && (select.Take != null || select.Skip != null || !selectIsNameMapProjection || selectHasGroupBy || selectHasAggregates || (selectHasOrderBy && !isTopLevel)))
                return false;

            return true;
        }

        private static bool IsNameMapProjection(SelectExpression select)
        {
            var fromSelect = select.From as SelectExpression;
            if (select.Fields.Count == 0)
                return true;

            if (fromSelect == null || select.Fields.Count != fromSelect.Fields.Count)
                return false;

            for (int i = 0, n = select.Fields.Count; i < n; i++)
            {
                if (select.Fields[i].Name != fromSelect.Fields[i].Name)
                    return false;
            }

            return true;
        }

        private static bool IsSimpleProjection(SelectExpression select)
        {
            return select.Fields.All(field => !string.IsNullOrEmpty(field.Name));
        }
    }
}