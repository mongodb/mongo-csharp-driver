using System;

namespace MongoDB.Linq.Expressions
{
    internal abstract class AliasedExpression : MongoExpression
    {
        public Alias Alias { get; private set; }

        protected AliasedExpression(MongoExpressionType nodeType, Type type, Alias alias)
            : base(nodeType, type)
        {
            Alias = alias;
        }
    }
}
