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
        }

        // public methods
        /// <summary>
        /// Pretty prints an Expression.
        /// </summary>
        /// <param name="exp">The Expression to pretty print.</param>
        /// <returns>A string containing the pretty printed Expression.</returns>
        public string PrettyPrint(Expression exp)
        {
            _sb = new StringBuilder();
            _indentation = "";
            Visit(exp);
            return _sb.ToString();
        }

        // protected methods
        /// <summary>
        /// Visits an ElementInit node.
        /// </summary>
        /// <param name="initializer">The ElementInit node.</param>
        /// <returns>The ElementInit node.</returns>
        protected override ElementInit VisitElementInitializer(ElementInit initializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a UnaryExpression.
        /// </summary>
        /// <param name="u">The UnaryExpression.</param>
        /// <returns>The UnaryExpression.</returns>
        protected override Expression VisitUnary(UnaryExpression u)
        {
            WriteHeader(u);
            using (new Indentation(this))
            {
                WriteLine("Method={0}", u.Method == null ? "null" : u.Method.Name);
                WriteLine("Operand:");
                VisitIndented(u.Operand);
            }
            return u;
        }

        /// <summary>
        /// Visits a BinaryExpression.
        /// </summary>
        /// <param name="b">The BinaryExpression.</param>
        /// <returns>The BinaryExpression.</returns>
        protected override Expression VisitBinary(BinaryExpression b)
        {
            WriteHeader(b);
            using (new Indentation(this))
            {
                WriteLine("Method={0}", b.Method == null ? "null" : b.Method.Name);
                WriteLine("Left:");
                VisitIndented(b.Left);
                WriteLine("Right:");
                VisitIndented(b.Right);
            }
            return b;
        }

        /// <summary>
        /// Visits a TypeBinaryExpression.
        /// </summary>
        /// <param name="b">The TypeBinaryExpression.</param>
        /// <returns>The TypeBinaryExpression.</returns>
        protected override Expression VisitTypeIs(TypeBinaryExpression b)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a ConstantExpression.
        /// </summary>
        /// <param name="c">The ConstantExpression.</param>
        /// <returns>The ConstantExpression.</returns>
        protected override Expression VisitConstant(ConstantExpression c)
        {
            WriteHeader(c);
            using (new Indentation(this))
            {
                WriteLine("Value={0}", c.Value);
            }
            return c;
        }

        /// <summary>
        /// Visits a ConditionalExpression.
        /// </summary>
        /// <param name="c">The ConditionalExpression.</param>
        /// <returns>The ConditionalExpression.</returns>
        protected override Expression VisitConditional(ConditionalExpression c)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a ParameterExpression.
        /// </summary>
        /// <param name="p">The ParameterExpression.</param>
        /// <returns>The ParameterExpression.</returns>
        protected override Expression VisitParameter(ParameterExpression p)
        {
            WriteHeader(p);
            using (new Indentation(this))
            {
                WriteLine("Name={0}", p.Name);
                WriteLine("Type={0}", FriendlyClassName(p.Type));
            }
            return p;
        }

        /// <summary>
        /// Visits a MemberExpression.
        /// </summary>
        /// <param name="m">The MemberExpression.</param>
        /// <returns>The MemberExpression.</returns>
        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            WriteHeader(m);
            using (new Indentation(this))
            {
                WriteLine("Expression:");
                VisitIndented(m.Expression);
                WriteLine("Member={0} {1}", m.Member.MemberType, m.Member.Name);
            }
            return m;
        }

        /// <summary>
        /// Visits a MethodCallExpression.
        /// </summary>
        /// <param name="m">The MethodCallExpression.</param>
        /// <returns>The MethodCallExpression.</returns>
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            WriteHeader(m);
            using (new Indentation(this))
            {
                WriteLine("Method={0}", m.Method.Name);
                WriteLine("Arguments:");
                using (new Indentation(this))
                {
                    foreach (var arg in m.Arguments)
                    {
                        Visit(arg);
                    }
                }
            }
            return m;
        }

        /// <summary>
        /// Visits an Expression list.
        /// </summary>
        /// <param name="original">The Expression list.</param>
        /// <returns>The Expression list.</returns>
        protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberAssignment.
        /// </summary>
        /// <param name="assignment">The MemberAssignment.</param>
        /// <returns>The MemberAssignment.</returns>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberMemberBinding.
        /// </summary>
        /// <param name="binding">The MemberMemberBinding.</param>
        /// <returns>The MemberMemberBinding.</returns>
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberListBinding.
        /// </summary>
        /// <param name="binding">The MemberListBinding.</param>
        /// <returns>The MemberListBinding.</returns>
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberBinding list.
        /// </summary>
        /// <param name="original">The MemberBinding list.</param>
        /// <returns>The MemberBinding list.</returns>
        protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits an ElementInit list.
        /// </summary>
        /// <param name="original">The ElementInit list.</param>
        /// <returns>The ElementInit list.</returns>
        protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a LambdaExpression.
        /// </summary>
        /// <param name="lambda">The LambdaExpression.</param>
        /// <returns>The LambdaExpression.</returns>
        protected override Expression VisitLambda(LambdaExpression lambda)
        {
            WriteHeader(lambda);
            using (new Indentation(this))
            {
                WriteLine("Parameters:");
                foreach (var parameter in lambda.Parameters)
                {
                    VisitIndented(parameter);
                }
                WriteLine("Body:");
                VisitIndented(lambda.Body);
            }
            return lambda;
        }

        /// <summary>
        /// Visits a NewExpression.
        /// </summary>
        /// <param name="nex">The NewExpression.</param>
        /// <returns>The NewExpression.</returns>
        protected override NewExpression VisitNew(NewExpression nex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a MemberInitExpression.
        /// </summary>
        /// <param name="init">The MemberInitExpression.</param>
        /// <returns>The MemberInitExpression.</returns>
        protected override Expression VisitMemberInit(MemberInitExpression init)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a ListInitExpression.
        /// </summary>
        /// <param name="init">The ListInitExpression.</param>
        /// <returns>The ListInitExpression.</returns>
        protected override Expression VisitListInit(ListInitExpression init)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits a NewArrayExpression.
        /// </summary>
        /// <param name="na">The NewArrayExpression.</param>
        /// <returns>The NewArrayExpression.</returns>
        protected override Expression VisitNewArray(NewArrayExpression na)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Visits an InvocationExpression.
        /// </summary>
        /// <param name="iv">The InvocationExpression.</param>
        /// <returns>The InvocationExpression.</returns>
        protected override Expression VisitInvocation(InvocationExpression iv)
        {
            throw new NotImplementedException();
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

        private void VisitIndented(Expression exp)
        {
            using (new Indentation(this))
            {
                Visit(exp);
            }
        }

        private void WriteHeader(Expression exp)
        {
            WriteLine("{0}:{1} Type={2}", PublicClassName(exp.GetType()), exp.NodeType, FriendlyClassName(exp.Type));
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
