using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class QueryDuplicator : MongoExpressionVisitor
    {
        Dictionary<Alias, Alias> _map;

        public Expression Duplicate(Expression expression)
        {
            _map = new Dictionary<Alias, Alias>();
            return Visit(expression);
        }

        protected override Expression VisitCollection(CollectionExpression collection)
        {
            var newAlias = new Alias();
            _map[collection.Alias] = newAlias;
            return new CollectionExpression(newAlias, collection.Database, collection.CollectionName, collection.DocumentType);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            var newAlias = new Alias();
            _map[select.Alias] = newAlias;
            select = (SelectExpression)base.VisitSelect(select);
            return new SelectExpression(newAlias, select.Fields, select.From, select.Where, select.OrderBy, select.GroupBy, select.IsDistinct, select.Skip, select.Take);
        }

        protected override Expression VisitField(FieldExpression field)
        {
            Alias newAlias;
            if (_map.TryGetValue(field.Alias, out newAlias))
                return new FieldExpression(field.Expression, newAlias, field.Name);

            return field;
        }
    }
}
