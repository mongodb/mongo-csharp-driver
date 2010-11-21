using System;

namespace MongoDB.Linq.Expressions
{
    internal abstract class SubqueryExpression : MongoExpression
    {
        public SelectExpression Select { get; private set; }

        protected SubqueryExpression(MongoExpressionType nodeType, Type type, SelectExpression select)
            : base(nodeType, type)
        {
            Select = select;
        }
    }
}
