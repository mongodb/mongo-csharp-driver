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
using System.Linq.Expressions;
using System.Reflection;

namespace MongoDB.Driver.Linq
{
    internal class ExpressionNormalizer : ExpressionVisitor
    {
        private ExpressionNormalizer()
        {
        }

        public static Expression Normalize(Expression node)
        {
            var normalizer = new ExpressionNormalizer();
            return normalizer.Visit(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            node = EnsureConstantIsOnRight(node);

            // VB lifts comparisons of Nullable<T> values and C# does not, so unlift them
            if (node.Type == typeof(Nullable<bool>))
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.NotEqual:
                        node = Expression.MakeBinary(
                            node.NodeType,
                            node.Left,
                            node.Right,
                            false, // liftToNull
                            null, // method
                            null); // conversion
                        break;
                }
            }

            Expression result = null;
            if (node.Left.NodeType == ExpressionType.Call && node.Right.NodeType == ExpressionType.Constant)
            {
                var mex = (MethodCallExpression)node.Left;
                var constant = (ConstantExpression)node.Right;
                if (mex.Method.DeclaringType.FullName == "Microsoft.VisualBasic.CompilerServices.Operators")
                {
                    // VB creates expression trees with "special" operators
                    result = VisitVBCompilerServicesOperators(mex, node.NodeType, constant);
                }
                else if (mex.Method.DeclaringType == typeof(string) && mex.Method.Name == "get_Chars" && constant.Type == typeof(char))
                {
                    // VB creates string index expressions using character comparison whereas C# uses ascii value comparison
                    // so, we make VB's string index comparison look like C#
                    result = Expression.MakeBinary(
                        node.NodeType,
                        Expression.Convert(mex, typeof(int)),
                        Expression.Constant(Convert.ToInt32((char)constant.Value)));
                }
            }

            // VB introduces a Convert on the LHS with a Nothing comparison, so we make it look like C# which does not have
            // any with a comparison to null
            if ((node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual) && 
                node.Left.NodeType == ExpressionType.Convert && 
                node.Right.NodeType == ExpressionType.Constant)
            {
                var left = (UnaryExpression)node.Left;
                var right = (ConstantExpression)node.Right;
                if (left.Type == typeof(object) && right.Value == null)
                {
                    result = Expression.MakeBinary(
                        node.NodeType,
                        left.Operand,
                        node.Right);
                }
            }

            // VB creates coalescing operations when dealing with nullable value comparisons, so we try and make this look like C#
            if (node.NodeType == ExpressionType.Coalesce)
            {
                var nodeLeftTypeInfo = node.Left.Type.GetTypeInfo();
                var right = node.Right as ConstantExpression;
                if (node.Left.NodeType == ExpressionType.Equal &&
                    nodeLeftTypeInfo.IsGenericType &&
                    nodeLeftTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    right != null &&
                    right.Type == typeof(bool) &&
                    (bool)right.Value == false)
                {
                    node = (BinaryExpression)node.Left;
                    return Expression.MakeBinary(
                        ExpressionType.Equal,
                        Visit(node.Left),
                        Visit(node.Right),
                        false,
                        null);
                }
            }

            if (result != null)
            {
                return result;
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var newNode = base.VisitUnary(node);

            if (newNode.NodeType == ExpressionType.Convert || newNode.NodeType == ExpressionType.ConvertChecked)
            {
                var newUnaryNode = (UnaryExpression)newNode;
                var targetType = newUnaryNode.Type;
                var sourceType = newUnaryNode.Operand.Type;

                // get rid of a completely useless conversion.  This is caused by the removal
                // of unlifting a binary expression that VB inserts into the expression tree.
                if (targetType == sourceType)
                {
                    return newUnaryNode.Operand;
                }

                // VB may add in extra casts that are unnecessary.  We'll remove the one in the middle.
                if (newUnaryNode.Operand.NodeType == ExpressionType.Convert || newUnaryNode.Operand.NodeType == ExpressionType.ConvertChecked)
                {
                    var nestedUnaryNode = (UnaryExpression)newUnaryNode.Operand;
                    return Expression.MakeUnary(newUnaryNode.NodeType, nestedUnaryNode.Operand, newNode.Type);
                }

                // VB really screws up some things by converting strings into enumerable chars...
                if (targetType == typeof(IEnumerable<char>) && sourceType == typeof(string))
                {
                    return newUnaryNode.Operand;
                }
            }

            return newNode;
        }

        private BinaryExpression EnsureConstantIsOnRight(BinaryExpression node)
        {
            var left = node.Left;
            var right = node.Right;
            var operatorType = node.NodeType;
            if (left.NodeType == ExpressionType.Constant)
            {
                right = node.Left;
                left = node.Right;
                // if the constant was on the left some operators need to be flipped
                switch (operatorType)
                {
                    case ExpressionType.LessThan: operatorType = ExpressionType.GreaterThan; break;
                    case ExpressionType.LessThanOrEqual: operatorType = ExpressionType.GreaterThanOrEqual; break;
                    case ExpressionType.GreaterThan: operatorType = ExpressionType.LessThan; break;
                    case ExpressionType.GreaterThanOrEqual: operatorType = ExpressionType.LessThanOrEqual; break;
                }
            }

            if (left != node.Left || right != node.Right || operatorType != node.NodeType)
            {
                return Expression.MakeBinary(operatorType, left, right);
            }

            return node;
        }

        private Expression VisitVBCompilerServicesOperators(MethodCallExpression mex, ExpressionType expressionType, ConstantExpression constant)
        {
            if (mex.Method.Name == "CompareString" && constant.Type == typeof(Int32))
            {
                return VisitVBCompilerServicesOperatorsCompareString(mex, expressionType, (int)constant.Value);
            }

            return null;
        }

        private Expression VisitVBCompilerServicesOperatorsCompareString(MethodCallExpression mex, ExpressionType expressionType, int comparisonValue)
        {
            if (comparisonValue == 0)
            {
                return Expression.MakeBinary(
                    expressionType,
                    mex.Arguments[0],
                    mex.Arguments[1]);
            }

            return null;
        }
    }
}