using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class DeclaredAliasGatherer : MongoExpressionVisitor
    {
        private HashSet<Alias> _aliases;

        public HashSet<Alias> Gather(Expression source)
        {
            _aliases = new HashSet<Alias>();
            Visit(source);
            return _aliases;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            _aliases.Add(select.Alias);
            return select;
        }

        protected override Expression VisitCollection(CollectionExpression collection)
        {
            _aliases.Add(collection.Alias);
            return collection;
        }
    }
}
