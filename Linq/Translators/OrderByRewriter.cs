using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Linq.Expressions;
using System.Collections.ObjectModel;

namespace MongoDB.Linq.Translators
{
    internal class OrderByRewriter : MongoExpressionVisitor
    {
        private IList<OrderExpression> _gatheredOrderings;
        private HashSet<string> _uniqueColumns;
        private bool _isOutermostSelect;

        public Expression Rewrite(Expression expression)
        {
            _isOutermostSelect = true;
            return Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            var saveIsOuterMostSelect = _isOutermostSelect;
            try
            {
                _isOutermostSelect = false;
                select = (SelectExpression)base.VisitSelect(select);

                var hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
                var hasGroupBy = select.GroupBy != null;
                var canHaveOrderings = saveIsOuterMostSelect || select.Take != null || select.Skip != null;
                var canReceivedOrderings = canHaveOrderings && !hasGroupBy && !select.IsDistinct;

                if (hasOrderBy)
                    PrependOrderings(select.OrderBy);

                IEnumerable<OrderExpression> orderings = null;
                if (canReceivedOrderings)
                    orderings = _gatheredOrderings;
                else if (canHaveOrderings)
                    orderings = select.OrderBy;

                var canPassOnOrderings = !saveIsOuterMostSelect && !hasGroupBy && !select.IsDistinct;
                ReadOnlyCollection<FieldDeclaration> fields = select.Fields;
                if (_gatheredOrderings != null)
                {
                    if (canPassOnOrderings)
                    {
                        var producedAliases = new DeclaredAliasGatherer().Gather(select.From);

                        BindResult project = RebindOrderings(_gatheredOrderings, select.Alias, producedAliases, select.Fields);
                        _gatheredOrderings = null;
                        PrependOrderings(project.Orderings);
                        fields = project.Fields;
                    }
                    else
                        _gatheredOrderings = null;
                }
                if (orderings != select.OrderBy || fields != select.Fields)
                    select = new SelectExpression(select.Alias, fields, select.From, select.Where, orderings, select.GroupBy, select.IsDistinct, select.Skip, select.Take);

                return select;
            }
            finally
            {
                _isOutermostSelect = saveIsOuterMostSelect;
            }
        }

        protected override Expression VisitSubquery(SubqueryExpression subquery)
        {
            var saveOrderings = _gatheredOrderings;
            _gatheredOrderings = null;
            var result = base.VisitSubquery(subquery);
            _gatheredOrderings = saveOrderings;
            return result;
        }

        private void PrependOrderings(IList<OrderExpression> newOrderings)
        {
            if(newOrderings == null)
                return;
            
            if (_gatheredOrderings == null)
            {
                _gatheredOrderings = new List<OrderExpression>();
                _uniqueColumns = new HashSet<string>();
            }

            for (int i = newOrderings.Count - 1; i >= 0; i--)
            {
                var ordering = newOrderings[i];
                var field = ordering.Expression as FieldExpression;
                if (field != null)
                {
                    var hash = field.Alias + ":" + field.Name;
                    if (!_uniqueColumns.Contains(hash))
                    {
                        _gatheredOrderings.Insert(0, ordering);
                        _uniqueColumns.Add(hash);
                    }
                }
                else
                    _gatheredOrderings.Insert(0, ordering);
            }
        }

        private BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, Alias alias, ICollection<Alias> existingAliases, IEnumerable<FieldDeclaration> existingFields)
        {
            List<FieldDeclaration> newFields = null;
            var newOrderings = new List<OrderExpression>();
            foreach (var ordering in orderings)
            {
                var expression = ordering.Expression;
                var field = expression as FieldExpression;

                if(field != null && (existingAliases == null || !existingAliases.Contains(field.Alias)))
                    continue;
                
                int ordinal = 0;
                foreach (var fieldDecl in existingFields)
                {
                    var fieldDeclExpression = fieldDecl.Expression as FieldExpression;
                    if (fieldDecl.Expression == ordering.Expression || (field != null && fieldDeclExpression != null && field.Alias == fieldDeclExpression.Alias && field.Name == fieldDeclExpression.Name))
                    {
                        if(field != null)
                            expression = new FieldExpression(field.Expression, alias, fieldDecl.Name);
                        break;
                    }
                    ordinal++;
                }

                if (expression == ordering.Expression)
                {
                    if (newFields == null)
                    {
                        newFields = new List<FieldDeclaration>(existingFields);
                        existingFields = newFields;
                    }

                    var fieldName = field != null ? field.Name : "_$f" + ordinal;
                    newFields.Add(new FieldDeclaration(fieldName, ordering.Expression));
                    expression = new FieldExpression(expression, alias, fieldName);
                }

                newOrderings.Add(new OrderExpression(ordering.OrderType, expression));
            }
            return new BindResult(existingFields, newOrderings);
        }

        private class BindResult
        {
            public ReadOnlyCollection<FieldDeclaration> Fields { get; private set; }

            public ReadOnlyCollection<OrderExpression> Orderings { get; private set; }

            public BindResult(IEnumerable<FieldDeclaration> fields, IEnumerable<OrderExpression> orderings)
            {
                Fields = fields as ReadOnlyCollection<FieldDeclaration> ?? new List<FieldDeclaration>(fields).AsReadOnly();
                Orderings = orderings as ReadOnlyCollection<OrderExpression> ?? new List<OrderExpression>(orderings).AsReadOnly();
            }
        }
    }
}