using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class FieldMapper : MongoExpressionVisitor
    {
        private HashSet<Alias> _oldAliases;
        private Alias _newAlias;

        public Expression Map(Expression expression, Alias newAlias, IEnumerable<Alias> oldAliases)
        {
            _oldAliases = new HashSet<Alias>(oldAliases);
            _newAlias = newAlias;
            return Visit(expression);
        }

        public Expression Map(Expression expression, Alias newAlias, params Alias[] oldAliases)
        {
            return Map(expression, newAlias, (IEnumerable<Alias>)oldAliases);
        }

        protected override Expression VisitField(FieldExpression field)
        {
            if (_oldAliases.Contains(field.Alias))
                return new FieldExpression(field.Expression, _newAlias, field.Name);
            return field;
        }
    }
}