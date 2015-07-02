/* Copyright 2010-2015 MongoDB Inc.
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
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// An abstract base class for an Expression visitor that returns a value of type T.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    public abstract class ExpressionVisitor<T>
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the ExpressionVisitor class.
        /// </summary>
        protected ExpressionVisitor()
        {
        }

        // protected methods
        /// <summary>
        /// Visits an Expression.
        /// </summary>
        /// <param name="node">The Expression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T Visit(Expression node)
        {
            if (node == null)
            {
                return default(T);
            }
            switch (node.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return this.VisitUnary((UnaryExpression)node);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return this.VisitBinary((BinaryExpression)node);
                case ExpressionType.TypeIs:
                    return this.VisitTypeBinary((TypeBinaryExpression)node);
                case ExpressionType.Conditional:
                    return this.VisitConditional((ConditionalExpression)node);
                case ExpressionType.Constant:
                    return this.VisitConstant((ConstantExpression)node);
                case ExpressionType.Parameter:
                    return this.VisitParameter((ParameterExpression)node);
                case ExpressionType.MemberAccess:
                    return this.VisitMember((MemberExpression)node);
                case ExpressionType.Call:
                    return this.VisitMethodCall((MethodCallExpression)node);
                case ExpressionType.Lambda:
                    return this.VisitLambda((LambdaExpression)node);
                case ExpressionType.New:
                    return this.VisitNew((NewExpression)node);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return this.VisitNewArray((NewArrayExpression)node);
                case ExpressionType.Invoke:
                    return this.VisitInvocation((InvocationExpression)node);
                case ExpressionType.MemberInit:
                    return this.VisitMemberInit((MemberInitExpression)node);
                case ExpressionType.ListInit:
                    return this.VisitListInit((ListInitExpression)node);
                default:
                    throw new Exception(string.Format("Unhandled expression type: '{0}'", node.NodeType));
            }
        }

        /// <summary>
        /// Visits an Expression list.
        /// </summary>
        /// <param name="nodes">The Expression list.</param>
        /// <returns>The result of visiting the Expressions.</returns>
        protected T Visit(ReadOnlyCollection<Expression> nodes)
        {
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                this.Visit(nodes[i]);
            }
            return default(T);
        }

        /// <summary>
        /// Visits a BinaryExpression.
        /// </summary>
        /// <param name="node">The BinaryExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitBinary(BinaryExpression node)
        {
            this.Visit(node.Left);
            this.Visit(node.Right);
            this.Visit(node.Conversion);
            return default(T);
        }

        /// <summary>
        /// Visits a ConditionalExpression.
        /// </summary>
        /// <param name="node">The ConditionalExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitConditional(ConditionalExpression node)
        {
            this.Visit(node.Test);
            this.Visit(node.IfTrue);
            this.Visit(node.IfFalse);
            return default(T);
        }

        /// <summary>
        /// Visits a ConstantExpression.
        /// </summary>
        /// <param name="node">The ConstantExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitConstant(ConstantExpression node)
        {
            return default(T);
        }

        /// <summary>
        /// Visits an ElementInit.
        /// </summary>
        /// <param name="node">The ElementInit.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitElementInit(ElementInit node)
        {
            this.Visit(node.Arguments);
            return default(T);
        }

        // TODO: the .NET Framework 4 version of ExpressionVisitor does not have a method called VisitElementInitializerList
        // leaving this method for now, though perhaps it could be replaced with Visit(ReadOnlyCollection<Expression>)?

        /// <summary>
        /// Visits an ElementInit list.
        /// </summary>
        /// <param name="nodes">The ElementInit list.</param>
        /// <returns>The result of visiting the Expressions.</returns>
        protected T VisitElementInitList(
            ReadOnlyCollection<ElementInit> nodes)
        {
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                this.VisitElementInit(nodes[i]);
            }
            return default(T);
        }

        /// <summary>
        /// Visits an InvocationExpression.
        /// </summary>
        /// <param name="node">The InvocationExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitInvocation(InvocationExpression node)
        {
            this.Visit(node.Arguments);
            this.Visit(node.Expression);
            return default(T);
        }

        // TODO: in .NET Framework 4 VisitLambda takes an Expression<T> instead of Lambda
        // probably not worthing changing in our version of ExpressionVisitor

        /// <summary>
        /// Visits a LambdaExpression.
        /// </summary>
        /// <param name="node">The LambdaExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitLambda(LambdaExpression node)
        {
            this.Visit(node.Body);
            return default(T);
        }

        /// <summary>
        /// Visits a ListInitExpression.
        /// </summary>
        /// <param name="node">The ListInitExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitListInit(ListInitExpression node)
        {
            this.VisitNew(node.NewExpression);
            this.VisitElementInitList(node.Initializers);
            return default(T);
        }

        /// <summary>
        /// Visits a MemberExpression.
        /// </summary>
        /// <param name="node">The MemberExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitMember(MemberExpression node)
        {
            this.Visit(node.Expression);
            return default(T);
        }

        /// <summary>
        /// Visits a MemberAssignment.
        /// </summary>
        /// <param name="node">The MemberAssignment.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitMemberAssignment(MemberAssignment node)
        {
            this.Visit(node.Expression);
            return default(T);
        }

        /// <summary>
        /// Visits a MemberBinding.
        /// </summary>
        /// <param name="node">The MemberBinding.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitMemberBinding(MemberBinding node)
        {
            switch (node.BindingType)
            {
                case MemberBindingType.Assignment:
                    return this.VisitMemberAssignment((MemberAssignment)node);
                case MemberBindingType.MemberBinding:
                    return this.VisitMemberMemberBinding((MemberMemberBinding)node);
                case MemberBindingType.ListBinding:
                    return this.VisitMemberListBinding((MemberListBinding)node);
                default:
                    throw new Exception(string.Format("Unhandled binding type '{0}'", node.BindingType));
            }
        }

        // TODO: the .NET Framework 4 version of ExpressionVisitor does not have a method called VisitMemberBindingList
        // leaving this method for now, though perhaps it could be replaced with Visit(ReadOnlyCollection<Expression>)?

        /// <summary>
        /// Visits a MemberBinding list.
        /// </summary>
        /// <param name="nodes">The MemberBinding list.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitMemberBindingList(ReadOnlyCollection<MemberBinding> nodes)
        {
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                this.VisitMemberBinding(nodes[i]);
            }
            return default(T);
        }

        /// <summary>
        /// Visits a MemberInitExpression.
        /// </summary>
        /// <param name="node">The MemberInitExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitMemberInit(MemberInitExpression node)
        {
            this.VisitNew(node.NewExpression);
            this.VisitMemberBindingList(node.Bindings);
            return default(T);
        }

        /// <summary>
        /// Visits a MemberListBinding.
        /// </summary>
        /// <param name="node">The MemberListBinding.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitMemberListBinding(MemberListBinding node)
        {
            this.VisitElementInitList(node.Initializers);
            return default(T);
        }

        /// <summary>
        /// Visits a MemberMemberBinding.
        /// </summary>
        /// <param name="node">The MemberMemberBinding.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitMemberMemberBinding(MemberMemberBinding node)
        {
            this.VisitMemberBindingList(node.Bindings);
            return default(T);
        }

        /// <summary>
        /// Visits a MethodCallExpression.
        /// </summary>
        /// <param name="node">The MethodCallExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitMethodCall(MethodCallExpression node)
        {
            this.Visit(node.Object);
            this.Visit(node.Arguments);
            return default(T);
        }

        /// <summary>
        /// Visits a NewExpression.
        /// </summary>
        /// <param name="node">The NewExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitNew(NewExpression node)
        {
            this.Visit(node.Arguments);
            return default(T);
        }

        /// <summary>
        /// Visits a NewArrayExpression.
        /// </summary>
        /// <param name="node">The NewArrayExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitNewArray(NewArrayExpression node)
        {
            this.Visit(node.Expressions);
            return default(T);
        }

        /// <summary>
        /// Visits a ParameterExpression.
        /// </summary>
        /// <param name="node">The ParameterExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitParameter(ParameterExpression node)
        {
            return default(T);
        }

        /// <summary>
        /// Visits a TypeBinaryExpression.
        /// </summary>
        /// <param name="node">The TypeBinaryExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitTypeBinary(TypeBinaryExpression node)
        {
            this.Visit(node.Expression);
            return default(T);
        }

        /// <summary>
        /// Visits a UnaryExpression.
        /// </summary>
        /// <param name="node">The UnaryExpression.</param>
        /// <returns>The result of visiting the Expression.</returns>
        protected virtual T VisitUnary(UnaryExpression node)
        {
            this.Visit(node.Operand);
            return default(T);
        }
    }
}