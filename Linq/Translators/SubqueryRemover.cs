using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class SubqueryRemover : MongoExpressionVisitor
    {
        private HashSet<SelectExpression> _selectsToRemove;

        public Expression Remove(SelectExpression outerSelect, IEnumerable<SelectExpression> selectsToRemove)
        {
            _selectsToRemove = new HashSet<SelectExpression>(selectsToRemove);
            return Visit(outerSelect);
        }

        protected override Expression VisitSelect(SelectExpression s)
        {
            return _selectsToRemove.Contains(s) ? Visit(s.From) : base.VisitSelect(s);
        }
    }
}
