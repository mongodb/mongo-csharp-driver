/* Copyright 2010-2011 10gen Inc.
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
    public class ExpressionPrettyPrinter : ExpressionVisitor
    {
        // private fields
        private StringBuilder sb;
        private string indentation = "";

        // constructors
        public ExpressionPrettyPrinter()
        {
        }

        // public methods
        public string PrettyPrint(Expression exp)
        {
            sb = new StringBuilder();
            indentation = "";
            Visit(exp);
            return sb.ToString();
        }

        // protected methods
        protected override ElementInit VisitElementInitializer(ElementInit initializer)
        {
            throw new NotImplementedException();
        }

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

        protected override Expression VisitTypeIs(TypeBinaryExpression b)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            WriteHeader(c);
            using (new Indentation(this))
            {
                WriteLine("Value={0}", c.Value);
            }
            return c;
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            throw new NotImplementedException();
        }

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

        protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            throw new NotImplementedException();
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
        {
            throw new NotImplementedException();
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
        {
            throw new NotImplementedException();
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
        {
            throw new NotImplementedException();
        }

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

        protected override NewExpression VisitNew(NewExpression nex)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitMemberInit(MemberInitExpression init)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitListInit(ListInitExpression init)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitNewArray(NewArrayExpression na)
        {
            throw new NotImplementedException();
        }

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
            sb.Append(indentation);
            sb.Append(line);
            sb.AppendLine();
        }

        private void WriteLine(string format, params object[] args)
        {
            sb.Append(indentation);
            sb.AppendFormat(format, args);
            sb.AppendLine();
        }

        // nested classes
        internal class Indentation : IDisposable
        {
            // private fields
            private ExpressionPrettyPrinter prettyPrinter;

            // constructors
            public Indentation(ExpressionPrettyPrinter prettyPrinter)
            {
                this.prettyPrinter = prettyPrinter;
                prettyPrinter.indentation += "| ";
            }

            // public methods
            public void Dispose()
            {
                prettyPrinter.indentation = prettyPrinter.indentation.Remove(prettyPrinter.indentation.Length - 2);
            }
        }
    }
}
