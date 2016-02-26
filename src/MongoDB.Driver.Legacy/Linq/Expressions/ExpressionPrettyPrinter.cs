/* Copyright 2010-2016 MongoDB Inc.
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
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A class that pretty prints an Expression.
    /// </summary>
    public class ExpressionPrettyPrinter : ExpressionVisitor
    {
        // private fields
        private StringBuilder _sb;
        private string _indentation = "";

        // constructors
        /// <summary>
        /// Initializes a new instance of the ExpressionPrettyPrinter class.
        /// </summary>
        public ExpressionPrettyPrinter()
        {
            _sb = new StringBuilder();
            _indentation = "";
        }

        // public methods
        /// <summary>
        /// Pretty prints an Expression.
        /// </summary>
        /// <param name="node">The Expression to pretty print.</param>
        /// <returns>A string containing the pretty printed Expression.</returns>
        public static string PrettyPrint(Expression node)
        {
            var prettyPrinter = new ExpressionPrettyPrinter();
            prettyPrinter.Visit(node);
            return prettyPrinter.ToString();
        }

        /// <summary>
        /// Returns the pretty printed string representation of the Expression.
        /// </summary>
        /// <returns>The pretty printed string representation of the Expression.</returns>
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
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("Method={0}", node.Method == null ? "null" : node.Method.Name);
                WriteLine("Left:");
                VisitIndented(node.Left);
                WriteLine("Right:");
                VisitIndented(node.Right);
            }
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
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("Value={0}", node.Value);
            }
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
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("Parameters:");
                foreach (var parameter in node.Parameters)
                {
                    VisitIndented(parameter);
                }
                WriteLine("Body:");
                VisitIndented(node.Body);
            }
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
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("Expression:");
                VisitIndented(node.Expression);
                WriteLine("Member={0} {1}", node.Member.MemberType, node.Member.Name);
            }
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
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("Method={0}", node.Method.Name);
                if (node.Object == null)
                {
                    WriteLine("Object=null");
                }
                else
                {
                    WriteLine("Object:");
                    VisitIndented(node.Object);
                }
                WriteLine("Arguments:");
                using (new Indentation(this))
                {
                    foreach (var arg in node.Arguments)
                    {
                        Visit(arg);
                    }
                }
            }
            return node;
        }

        /// <summary>
        /// Visits a NewExpression.
        /// </summary>
        /// <param name="node">The NewExpression.</param>
        /// <returns>The NewExpression.</returns>
        protected override NewExpression VisitNew(NewExpression node)
        {
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("Arguments:");
                foreach (var arg in node.Arguments)
                {
                    VisitIndented(arg);
                }
            }
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
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("Name={0}", node.Name);
                WriteLine("Type={0}", FriendlyClassName(node.Type));
            }
            return node;
        }

        /// <summary>
        /// Visits a TypeBinaryExpression.
        /// </summary>
        /// <param name="node">The TypeBinaryExpression.</param>
        /// <returns>The TypeBinaryExpression.</returns>
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("TypeOperand={0}", FriendlyClassName(node.TypeOperand));
                WriteLine("Expression:");
                VisitIndented(node.Expression);
            }
            return node;
        }

        /// <summary>
        /// Visits a UnaryExpression.
        /// </summary>
        /// <param name="node">The UnaryExpression.</param>
        /// <returns>The UnaryExpression.</returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            WriteHeader(node);
            using (new Indentation(this))
            {
                WriteLine("Method={0}", node.Method == null ? "null" : node.Method.Name);
                WriteLine("Operand:");
                VisitIndented(node.Operand);
            }
            return node;
        }

        // private methods
        private string FriendlyClassName(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                return typeInfo.Name;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0}<", Regex.Replace(typeInfo.Name, @"\`\d+$", ""));
            foreach (var typeParameter in typeInfo.GetGenericArguments())
            {
                sb.AppendFormat("{0}, ", FriendlyClassName(typeParameter));
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append(">");
            return sb.ToString();
        }

        private string PublicClassName(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (!typeInfo.IsPublic)
            {
                type = typeInfo.BaseType;
            }
            return FriendlyClassName(type);
        }

        private void VisitIndented(Expression node)
        {
            using (new Indentation(this))
            {
                Visit(node);
            }
        }

        private void WriteHeader(Expression node)
        {
            WriteLine("{0}:{1} Type={2}", PublicClassName(node.GetType()), node.NodeType, FriendlyClassName(node.Type));
        }

        private void WriteLine(string line)
        {
            _sb.Append(_indentation);
            _sb.Append(line);
            _sb.AppendLine();
        }

        private void WriteLine(string format, params object[] args)
        {
            _sb.Append(_indentation);
            _sb.AppendFormat(format, args);
            _sb.AppendLine();
        }

        // nested classes
        internal class Indentation : IDisposable
        {
            // private fields
            private ExpressionPrettyPrinter _prettyPrinter;

            // constructors
            public Indentation(ExpressionPrettyPrinter prettyPrinter)
            {
                _prettyPrinter = prettyPrinter;
                prettyPrinter._indentation += "| ";
            }

            // public methods
            public void Dispose()
            {
                _prettyPrinter._indentation = _prettyPrinter._indentation.Remove(_prettyPrinter._indentation.Length - 2);
            }
        }
    }
}
