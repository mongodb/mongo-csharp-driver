using System.Linq.Expressions;

namespace MongoDB.Linq.Expressions
{
    internal class FieldDeclaration
    {
        private readonly string _name;
        private readonly Expression _expression;

        public string Name
        {
            get { return _name; }
        }

        public Expression Expression
        {
            get { return _expression; }
        }

        public FieldDeclaration(string name, Expression expression)
        {
            _name = name;
            _expression = expression;
        }

    }
}
