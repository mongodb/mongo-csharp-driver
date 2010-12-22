using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Driver.Builders.TypedBuilder
{
    public class ConstantEvaluator : ExpressionVisitor
    {
        private static readonly Dictionary<string, Func<object, object>> Cache =
            new Dictionary<string, Func<object, object>>();

        public static Expression Eval(Expression e)
        {
            return new ConstantEvaluator().Visit(e);
        }

        protected override Expression VisitMemberAccess(MemberExpression node)
        {
            var members = new List<MemberExpression>(5);
            var lastNode = TravenceMemberAccessExpressions(node, members);

            if (lastNode.NodeType != ExpressionType.Constant)
            {
                return base.VisitMemberAccess(node);
            }

            var key = string.Join(".", members.Select(q => q.Member.Name + q.Member.DeclaringType.FullName).ToArray());

            var accessor = GetAccessor(members, key);

            return Expression.Constant(accessor(((ConstantExpression)lastNode).Value));
        }

        private Func<object, object> GetAccessor(List<MemberExpression> members, string key)
        {
            Func<object, object> accessor;
            lock (Cache)
            {
                if (!Cache.TryGetValue(key, out accessor))
                {
                    accessor = CreateAccessor(members);
                    Cache.Add(key, accessor);
                }
            }
            return accessor;
        }

        private static Func<object, object> CreateAccessor(List<MemberExpression> members)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(object), "arg");
            Expression first = Expression.Convert(parameter, members.Last().Member.DeclaringType);

            first = Enumerable.Reverse(members).Aggregate(first, (current, memberExpression) => Expression.MakeMemberAccess(current, memberExpression.Member));
            first = Expression.Convert(first, typeof(object));

            Func<object, object> accessor = Expression.Lambda<Func<object, object>>(first, parameter).Compile();
            return accessor;
        }

        private static Expression TravenceMemberAccessExpressions(MemberExpression node, List<MemberExpression> members)
        {
            Expression current = node;
            while (current.NodeType == ExpressionType.MemberAccess)
            {
                members.Add((MemberExpression)current);
                current = ((MemberExpression)current).Expression;
            }
            return current;
        }
    }
}