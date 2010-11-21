using System.Linq.Expressions;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class ExpressionReplacer : MongoExpressionVisitor
    {
        private Expression _replaceWith;
        private Expression _searchFor;

        public Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
        {
            _searchFor = searchFor;
            _replaceWith = replaceWith;
            return Visit(expression);
        }

        public Expression ReplaceAll(Expression expression, Expression[] searchFor, Expression[] replaceWith)
        {
            for(var i = 0; i < searchFor.Length; i++)
                expression = Replace(expression, searchFor[i], replaceWith[i]);
            return expression;
        }

        protected override Expression Visit(Expression exp)
        {
            return exp == _searchFor ? _replaceWith : base.Visit(exp);
        }
    }
}
