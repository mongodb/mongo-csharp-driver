/* Copyright 2010-2012 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A class that formats an Expression as a string.
    /// </summary>
    public class ExpressionFormatter : ExpressionVisitor
    {
        // private fields
        private StringBuilder _sb;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ExpressionPrettyPrinter class.
        /// </summary>
        public ExpressionFormatter()
        {
            _sb = new StringBuilder();
        }

        // public methods
        /// <summary>
        /// Pretty prints an Expression.
        /// </summary>
        /// <param name="node">The Expression to pretty print.</param>
        /// <returns>A string containing the pretty printed Expression.</returns>
        public static string ToString(Expression node)
        {
            var formatter = new ExpressionFormatter();
            formatter.Visit(node);
            return formatter.ToString();
        }

        /// <summary>
        /// Returns the pretty printed string representation of the Expression.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _sb.ToString();
        }

        // protected methods
        /// <summary>
        /// Visits a BinaryExpression.
        /// </summary>
        /// <param name="node">The BinaryExpression.</param>
        /// <returns>The BinaryExpression.</returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            _sb.Append("(");
            Visit(node.Left);
            switch (node.NodeType)
            {
                case ExpressionType.AndAlso: _sb.Append(" && "); break;
                case ExpressionType.Equal: _sb.Append(" == "); break;
                case ExpressionType.OrElse: _sb.Append(" || "); break;
                default: throw new InvalidOperationException("Unexpected NodeType.");
            }
            Visit(node.Right);
            _sb.Append(")");
            return node;
        }

        /// <summary>
        /// Visits a ConditionalExpression.
        /// </summary>
        /// <param name="node">The ConditionalExpression.</param>
        /// <returns>The ConditionalExpression.</returns>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a ConstantExpression.
        /// </summary>
        /// <param name="node">The ConstantExpression.</param>
        /// <returns>The ConstantExpression.</returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            _sb.Append(node.Value.ToString());
            return node;
        }

        /// <summary>
        /// Visits an ElementInit node.
        /// </summary>
        /// <param name="node">The ElementInit node.</param>
        /// <returns>The ElementInit node.</returns>
        protected override ElementInit VisitElementInit(ElementInit node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits an ElementInit list.
        /// </summary>
        /// <param name="nodes">The ElementInit list.</param>
        /// <returns>The ElementInit list.</returns>
        protected override IEnumerable<ElementInit> VisitElementInitList(
            ReadOnlyCollection<ElementInit> nodes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits an InvocationExpression.
        /// </summary>
        /// <param name="node">The InvocationExpression.</param>
        /// <returns>The InvocationExpression.</returns>
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a LambdaExpression.
        /// </summary>
        /// <param name="node">The LambdaExpression.</param>
        /// <returns>The LambdaExpression.</returns>
        protected override Expression VisitLambda(LambdaExpression node)
        {
            _sb.Append("(");
            _sb.Append(string.Join(", ", node.Parameters.Select(p => p.Type.Name + " " + p.Name).ToArray()));
            _sb.Append(") => ");
            Visit(node.Body);
            return node;
        }

        /// <summary>
        /// Visits a ListInitExpression.
        /// </summary>
        /// <param name="node">The ListInitExpression.</param>
        /// <returns>The ListInitExpression.</returns>
        protected override Expression VisitListInit(ListInitExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberExpression.
        /// </summary>
        /// <param name="node">The MemberExpression.</param>
        /// <returns>The MemberExpression.</returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            Visit(node.Expression);
            _sb.Append(".");
            _sb.Append(node.Member.Name);
            return node;
        }

        /// <summary>
        /// Visits a MemberAssignment.
        /// </summary>
        /// <param name="node">The MemberAssignment.</param>
        /// <returns>The MemberAssignment.</returns>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberBinding.
        /// </summary>
        /// <param name="node">The MemberBinding.</param>
        /// <returns>The MemberBinding (possibly modified).</returns>
        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberBinding list.
        /// </summary>
        /// <param name="nodes">The MemberBinding list.</param>
        /// <returns>The MemberBinding list.</returns>
        protected override IEnumerable<MemberBinding> VisitMemberBindingList(ReadOnlyCollection<MemberBinding> nodes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberInitExpression.
        /// </summary>
        /// <param name="node">The MemberInitExpression.</param>
        /// <returns>The MemberInitExpression.</returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberListBinding.
        /// </summary>
        /// <param name="node">The MemberListBinding.</param>
        /// <returns>The MemberListBinding.</returns>
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberMemberBinding.
        /// </summary>
        /// <param name="node">The MemberMemberBinding.</param>
        /// <returns>The MemberMemberBinding.</returns>
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MethodCallExpression.
        /// </summary>
        /// <param name="node">The MethodCallExpression.</param>
        /// <returns>The MethodCallExpression.</returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            _sb.Append(node.Method.Name);
            _sb.Append("(");
            var separator = "";
            foreach (var arg in node.Arguments)
            {
                _sb.Append(separator);
                Visit(arg);
                separator = ", ";
            }
            _sb.Append(")");
            return node;
        }

        /// <summary>
        /// Visits a NewExpression.
        /// </summary>
        /// <param name="node">The NewExpression.</param>
        /// <returns>The NewExpression.</returns>
        protected override NewExpression VisitNew(NewExpression node)
        {
            _sb.Append("new ");
            _sb.Append(node.Type.Name);
            _sb.Append("(");
            var separator = "";
            foreach (var arg in node.Arguments)
            {
                _sb.Append(separator);
                Visit(arg);
                separator = ", ";
            }
            _sb.Append(")");
            return node;
        }

        /// <summary>
        /// Visits a NewArrayExpression.
        /// </summary>
        /// <param name="node">The NewArrayExpression.</param>
        /// <returns>The NewArrayExpression.</returns>
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a ParameterExpression.
        /// </summary>
        /// <param name="node">The ParameterExpression.</param>
        /// <returns>The ParameterExpression.</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            _sb.Append(node.Name);
            return node;
        }

        /// <summary>
        /// Visits a TypeBinaryExpression.
        /// </summary>
        /// <param name="node">The TypeBinaryExpression.</param>
        /// <returns>The TypeBinaryExpression.</returns>
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a UnaryExpression.
        /// </summary>
        /// <param name="node">The UnaryExpression.</param>
        /// <returns>The UnaryExpression.</returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Negate: _sb.Append("-"); break;
                default: throw new InvalidOperationException("Unexpected NodeType.");
            }
            Visit(node.Operand);
            return node;
        }

        // private methods
        private string FriendlyClassName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0}<", Regex.Replace(type.Name, @"\`\d+$", ""));
            foreach (var typeParameter in type.GetGenericArguments())
            {
                sb.AppendFormat("{0}, ", FriendlyClassName(typeParameter));
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append(">");
            return sb.ToString();
        }

        private string PublicClassName(Type type)
        {
            while (!type.IsPublic)
            {
                type = type.BaseType;
            }
            return FriendlyClassName(type);
        }
    }
}
