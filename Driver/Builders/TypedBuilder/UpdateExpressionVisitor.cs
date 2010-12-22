using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;

namespace MongoDB.Driver.Builders.TypedBuilder
{
    internal class UpdateExpressionVisitor : ExpressionVisitor
    {
        private readonly Stack<string> _assigments = new Stack<string>();
        private readonly Stack<Type> _classes = new Stack<Type>();
        private readonly UpdateBuilder _updateBuilder = new UpdateBuilder();

        public UpdateBuilder UpdateBuilder
        {
            get { return _updateBuilder; }
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            _classes.Push(node.Type);
            Expression visitMemberInit = base.VisitMemberInit(node);
            _classes.Pop();
            return visitMemberInit;
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            var constantValue = node.Expression as ConstantExpression;
            if (constantValue != null)
            {
                _updateBuilder.Set(String.Join(".", _assigments.Reverse().ToArray()), BsonValue.Create(constantValue.Value));
            }
            else
            {
                return base.VisitMemberAssignment(node);
            }

            return node;
        }

      
        protected override MemberBinding VisitBinding(MemberBinding node)
        {
            var elementName = BsonClassMap.LookupClassMap(_classes.Peek()).GetMemberMap(node.Member.Name).ElementName;
            _assigments.Push(elementName);

            MemberBinding visitMemberBinding = base.VisitBinding(node);
            _assigments.Pop();
            return visitMemberBinding;
        }
    }
}