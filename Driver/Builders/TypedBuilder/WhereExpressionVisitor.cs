using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;

namespace MongoDB.Driver.Builders
{
    internal class WhereExpressionVisitor : ExpressionVisitor
    {
        private readonly Queue<string> _memberAccesors = new Queue<string>();
        private readonly Stack<ExpressionTreeElement> _queries = new Stack<ExpressionTreeElement>();

        public QueryComplete SearchQuery
        {
            get
            {
                return _queries.Any() ? _queries.Peek().Query : null;
            }
        }

        protected override Expression VisitMemberAccess(MemberExpression node)
        {
            var name = BsonClassMap.LookupClassMap(node.Member.DeclaringType).GetMemberMap(node.Member.Name).ElementName;
            _memberAccesors.Enqueue(name);
            return base.VisitMemberAccess(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _queries.Push(new ExpressionTreeElement(BsonValue.Create(node.Value)));
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name != "Contains")
            {
                throw new InvalidOperationException("Not supported method " + node.Method.Name);
            }

            var sourceEnumerableExpression = (ConstantExpression)node.Arguments[0];
            var values = sourceEnumerableExpression.Value as IEnumerable<object>;
            VisitAndPushToQueueIfMember(node.Arguments[1]);
            if (values.Any())
            {
                PushQuery(Query.In(_queries.Pop().MemberName, values.Select(BsonValue.Create).ToArray()));
            }
            else
            {
                _queries.Pop();
            }

            return node;
        }

        private void VisitAndPushToQueueIfMember(Expression expression)
        {
            _memberAccesors.Clear();
            Visit(expression);

            var e = expression as MemberExpression;
            if (e == null)
            {
                return;
            }


            string elementAccesor = String.Join(".", _memberAccesors.Reverse().ToArray());
            _queries.Push(new ExpressionTreeElement(elementAccesor));
        }

        private void PushQuery(QueryComplete query)
        {
            _queries.Push(new ExpressionTreeElement(query));
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            VisitAndPushToQueueIfMember(node.Right);
            VisitAndPushToQueueIfMember(node.Left);
            switch (node.NodeType)
            {
                case ExpressionType.GreaterThan:
                    PushQuery(Query.GT(_queries.Pop().MemberName, _queries.Pop().Value));
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    PushQuery(Query.GTE(_queries.Pop().MemberName, _queries.Pop().Value));
                    break;
                case ExpressionType.LessThan:
                    PushQuery(Query.LT(_queries.Pop().MemberName, _queries.Pop().Value));
                    break;
                case ExpressionType.LessThanOrEqual:
                    PushQuery(Query.LTE(_queries.Pop().MemberName, _queries.Pop().Value));
                    break;
                case ExpressionType.NotEqual:
                    PushQuery(Query.NE(_queries.Pop().MemberName, _queries.Pop().Value));
                    break;
                case ExpressionType.Equal:
                    PushQuery(Query.EQ(_queries.Pop().MemberName, _queries.Pop().Value));
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    PushQuery(Query.And(_queries.Pop().Query, _queries.Pop().Query));
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    PushQuery(Query.Or(_queries.Pop().Query, _queries.Pop().Query));
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Query operator type {0} is not supported",
                                                                      node.NodeType));
            }
            return node;
        }

        #region Nested type: ExpressopnTreeElement

        private class ExpressionTreeElement
        {
            private readonly string _memberName;
            private readonly QueryComplete _query;
            private readonly BsonValue _value;
            private readonly bool _isNull;

            private ExpressionTreeElement()
            {
                _isNull = true;
            }

            public ExpressionTreeElement(string memberName)
            {
                _memberName = memberName;
            }

            public ExpressionTreeElement(QueryComplete query)
            {
                _query = query;
            }

            public ExpressionTreeElement(BsonValue value)
            {
                _value = value;
            }

            public static ExpressionTreeElement NullElement
            {
                get { return new ExpressionTreeElement(); }
            }

            public QueryComplete Query
            {
                get
                {
                    if (!_isNull && _query == null)
                        throw new InvalidOperationException("Expected type Binary expression, but got instead " +
                                                            GetNodeType());
                    return _query;
                }
            }

            public BsonValue Value
            {
                get
                {
                    if (!_isNull && _value == null)
                        throw new InvalidOperationException("Expected type Constant value but got instead " +
                                                            GetNodeType());
                    return _value;
                }
            }

            public string MemberName
            {
                get
                {
                    if (!_isNull && _memberName == null)
                        throw new InvalidOperationException("Expected type Member accessor but got instead " +
                                                            GetNodeType());
                    return _memberName;
                }
            }

            private string GetNodeType()
            {
                if (_memberName != null)
                    return "Member accessor";
                if (_query != null)
                    return "Binary expression";
                if (_value != null)
                    return "Constant value";
                return "Unknown type";
            }
        }

        #endregion
    }
}