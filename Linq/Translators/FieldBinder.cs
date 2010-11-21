using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Linq.Expressions;
using MongoDB.Linq.Util;

namespace MongoDB.Linq.Translators
{
    internal class FieldBinder : ExpressionVisitor
    {
        private static readonly HashSet<Type> CollectionTypes = new HashSet<Type>
        {
            typeof(ICollection), typeof(ICollection<>)
        };

        private Alias _alias;
        private FieldFinder _finder;
        private Type _elementType;

        public Expression Bind(Expression expression, Type elementType)
        {
            _alias = new Alias();
            _finder = new FieldFinder();
            _elementType = elementType;
            return Visit(expression);
        }

        protected override Expression Visit(Expression exp)
        {
            if (exp == null)
                return exp;

            var fieldName = _finder.Find(exp);
            if (fieldName != null)
                return new FieldExpression(exp, _alias, fieldName);

            return base.Visit(exp);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            if(p.Type == _elementType)
                return new FieldExpression(p, _alias, "*");

            return base.VisitParameter(p);
        }

        private class FieldFinder : ExpressionVisitor
        {
            private Stack<string> _fieldParts;
            private bool _isBlocked;

            public string Find(Expression expression)
            {
                if (expression.NodeType == ExpressionType.Parameter)
                    return null;

                _fieldParts = new Stack<string>();
                _isBlocked = false;
                Visit(expression);
                var fieldName = string.Join(".", _fieldParts.ToArray());
                if (_isBlocked)
                    fieldName = null;

                return fieldName;
            }

            protected override Expression Visit(Expression exp)
            {
                if (exp == null)
                    return null;

                switch (exp.NodeType)
                {
                    case ExpressionType.ArrayIndex:
                    case ExpressionType.Call:
                    case ExpressionType.MemberAccess:
                    case ExpressionType.Parameter:
                        return base.Visit(exp);
                    default:
                        _isBlocked = true;
                        return exp;
                }
            }

            protected override Expression VisitBinary(BinaryExpression b)
            {
                //this is an ArrayIndex Node
                _fieldParts.Push(((int)((ConstantExpression)b.Right).Value).ToString());
                Visit(b.Left);
                return b;
            }

            protected override Expression VisitMemberAccess(MemberExpression m)
            {
                var declaringType = m.Member.DeclaringType;
                if (!IsNativeToMongo(declaringType) && !IsCollection(declaringType))
                {
                    var memberMap = BsonClassMap.LookupClassMap(declaringType).GetMemberMap(m.Member.Name);
                    _fieldParts.Push(memberMap.ElementName);
                    Visit(m.Expression);
                    return m;
                }

                _isBlocked = true;
                return m;
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Method.DeclaringType == typeof(Queryable) || m.Method.DeclaringType == typeof(Enumerable))
                {
                    if (m.Method.Name == "ElementAt" || m.Method.Name == "ElementAtOrDefault")
                    {
                        _fieldParts.Push(((int)((ConstantExpression)m.Arguments[1]).Value).ToString());
                        Visit(m.Arguments[0]);
                        return m;
                    }
                }
                else if (m.Method.DeclaringType == typeof(MongoQueryable))
                {
                    if (m.Method.Name == "Key")
                    {
                        _fieldParts.Push((string)((ConstantExpression)m.Arguments[1]).Value);
                        Visit(m.Arguments[0]);
                        return m;
                    }
                }
                else if (typeof(BsonDocument).IsAssignableFrom(m.Method.DeclaringType))
                {
                    if (m.Method.Name == "get_Item") //TODO: does this work for VB?
                    {
                        _fieldParts.Push((string)((ConstantExpression)m.Arguments[0]).Value);
                        Visit(m.Object);
                        return m;
                    }
                }
                else if (typeof(IList<>).IsOpenTypeAssignableFrom(m.Method.DeclaringType) || typeof(IList).IsAssignableFrom(m.Method.DeclaringType))
                {
                    if (m.Method.Name == "get_Item")
                    {
                        _fieldParts.Push(((int)((ConstantExpression)m.Arguments[0]).Value).ToString());
                        Visit(m.Object);
                        return m;
                    }
                }

                _isBlocked = true;
                return m;
            }

            //protected override Expression VisitParameter(ParameterExpression p)
            //{
            //    if (p.Type.IsGenericType && p.Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
            //        _isBlocked = true;
            //    return base.VisitParameter(p);
            //}

            private static bool IsCollection(Type type)
            {
                //HACK: this is going to generally subvert custom objects that implement ICollection or ICollection<T>, 
                //but are not collections
                if (type.IsGenericType)
                    type = type.GetGenericTypeDefinition();

                return CollectionTypes.Any(x => x.IsAssignableFrom(type));
            }

            private static bool IsNativeToMongo(Type type)
            {
                //TODO: this code exists here and in BsonClassMapDescriptor.  Should probably be centralized...
                var typeCode = Type.GetTypeCode(type);

                if (typeCode != TypeCode.Object)
                    return true;

                if (type == typeof(Guid))
                    return true;

                if (type == typeof(ObjectId))
                    return true;

                if (type == typeof(byte[]))
                    return true;

                return false;
            }
        }
    }
}