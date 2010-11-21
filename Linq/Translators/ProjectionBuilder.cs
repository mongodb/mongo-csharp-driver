using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Linq.Expressions;

namespace MongoDB.Linq.Translators
{
    internal class ProjectionBuilder : MongoExpressionVisitor
    {
        private bool _isMapReduce;
        private ParameterExpression _document;
        private readonly GroupingKeyDeterminer _determiner;

        public ProjectionBuilder()
        {
            _determiner = new GroupingKeyDeterminer();
        }

        public LambdaExpression Build(Expression projector, Type documentType, string parameterName, bool isMapReduce)
        {
            _isMapReduce = isMapReduce;
            if (_isMapReduce)
                _document = Expression.Parameter(typeof(BsonDocument), parameterName);
            else
                _document = Expression.Parameter(documentType, parameterName);

            return Expression.Lambda(Visit(projector), _document);
        }

        protected override Expression VisitField(FieldExpression field)
        {
            if(!_isMapReduce)
                return Visit(field.Expression);
            
            var parts = field.Name.Split('.');

            bool isGroupingField = _determiner.IsGroupingKey(field);
            Expression current;
            if(parts.Contains("Key") && isGroupingField)
                current = _document;
            else
                current = Expression.TypeAs(Expression.Call(
                    _document,
                    "GetValue",
                    Type.EmptyTypes,
                    Expression.Constant("value")), typeof(BsonDocument));

            for(int i = 0, n = parts.Length; i < n; i++)
            {
                var type = i == n - 1 ? field.Type : typeof(BsonDocument);

                if(parts[i] == "Key" && isGroupingField)
                    parts[i] = "_id";

                current = Expression.TypeAs(Expression.Call(
                    current,
                    "GetValue",
                    Type.EmptyTypes,
                    Expression.Constant(parts[i])), typeof(BsonDocument));
            }

            return current;
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return _document;
        }

        private class GroupingKeyDeterminer : MongoExpressionVisitor
        {
            private bool _isGroupingKey;

            public bool IsGroupingKey(Expression exp)
            {
                _isGroupingKey = false;
                Visit(exp);
                return _isGroupingKey;
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null)
                    return exp;

                if (_isGroupingKey)
                    return exp;

                if (exp.Type.IsGenericType && exp.Type.GetGenericTypeDefinition() == typeof(Grouping<,>))
                {
                    _isGroupingKey = true;
                    return exp;
                }
                return base.Visit(exp);
            }
        }
    }
}