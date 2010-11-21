using System;
using System.Linq.Expressions;

namespace MongoDB.Linq.Expressions
{
    internal abstract class MongoExpression : Expression
    {
        protected MongoExpression(MongoExpressionType nodeType, Type type)
            : base((ExpressionType)nodeType, type)
        { }
    }
}