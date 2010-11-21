using System.Linq.Expressions;

namespace MongoDB.Linq.Expressions
{
    internal class FieldExpression : MongoExpression
    {
        public Alias Alias { get; private set; }

        public Expression Expression { get; private set; }

        public string Name { get; private set; }

        public FieldExpression(Expression expression, Alias alias, string name)
            : base(MongoExpressionType.Field, expression.Type)
        {
            Alias = alias;
            Expression = expression;
            Name = name;
        }
    }
}