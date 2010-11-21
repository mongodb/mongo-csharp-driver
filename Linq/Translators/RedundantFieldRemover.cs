using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Linq.Expressions;
using System.Collections;

namespace MongoDB.Linq.Translators
{
    internal class RedundantFieldRemover : MongoExpressionVisitor
    {
        private Dictionary<FieldExpression, FieldExpression> _map;

        public Expression Remove(Expression expression)
        {
            _map = new Dictionary<FieldExpression, FieldExpression>();
            return Visit(expression);
        }

        protected override Expression VisitField(FieldExpression field)
        {
            FieldExpression mapped;
            if (_map.TryGetValue(field, out mapped))
                return mapped;
            return field;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            select = (SelectExpression)base.VisitSelect(select);

            var fields = select.Fields.OrderBy(f => f.Name).ToList();
            var removed = new BitArray(fields.Count);
            var anyRemoved = false;
            for (int i = 0, n = fields.Count; i < n; i++)
            {
                var fi = fields[i];
                var fxi = new FieldExpression(fi.Expression, select.Alias, fi.Name);
                for (int j = i + 1; j < n; j++)
                {
                    if (!removed.Get(i))
                    {
                        FieldDeclaration fj = fields[j];
                        if (AreSameExpression(fi.Expression, fj.Expression))
                        {
                            var fxj = new FieldExpression(fj.Expression, select.Alias, fj.Name);
                            _map.Add(fxj, fxi);
                            removed.Set(j, true);
                            anyRemoved = true;
                        }
                    }
                }
            }

            if (anyRemoved)
            {
                var newFields = new List<FieldDeclaration>();
                for (int i = 0, n = fields.Count; i < n; i++)
                {
                    if (!removed.Get(i))
                        newFields.Add(fields[i]);
                }
                select = select.SetFields(newFields);
            }
            return select;
        }

        private bool AreSameExpression(Expression a, Expression b)
        {
            if (a == b)
                return true;
            var fa = a as FieldExpression;
            var fb = b as FieldExpression;
            return fa != null && fb != null && fa.Alias == fb.Alias && fa.Name == fb.Name;
        }
    }
}