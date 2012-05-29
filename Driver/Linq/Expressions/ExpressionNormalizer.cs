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
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A class that normalizes C# and VB expression trees.
    /// </summary>
    public class ExpressionNormalizer : ExpressionVisitor
    {
        // private fields
        private Expression _expression;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ExpressionNormalizer class.
        /// </summary>
        /// <param name="expression">The expression to be evaluated.</param>
        private ExpressionNormalizer(Expression expression)
        {
            _expression = expression;
        }

        // public methods
        /// <summary>
        /// Normalizes C# and VB expression trees.
        /// </summary>
        /// <param name="node">The expression to normalize.</param>
        /// <returns>The normalized expression.</returns>
        public static Expression Normalize(Expression node)
        {
            var normalizer = new ExpressionNormalizer(node);
            return normalizer.Visit(node);
        }

        // protected methods
        /// <summary>
        /// Visits a BinaryExpression.
        /// </summary>
        /// <param name="node">The BinaryExpression.</param>
        /// <returns>The BinaryExpression (possibly modified).</returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            node = EnsureConstantIsOnRight(node);

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

            // VB creates coalescing operations when dealing with nullable value comparisons, so we try and make this look like C#
            if (node.NodeType == ExpressionType.Coalesce)
            {
                var right = node.Right as ConstantExpression;
                if (node.Left.NodeType == ExpressionType.Equal &&
                    node.Left.Type.IsGenericType &&
                    node.Left.Type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
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

        /// <summary>
        /// Visits a UnaryExpression.
        /// </summary>
        /// <param name="node">The UnaryExpression.</param>
        /// <returns>The UnaryExpression (possibly modified).</returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
            {
                if (node.Type.IsAssignableFrom(node.Operand.Type))
                {
                    // ignore the unnecessary conversion added by VB
                    return Visit(node.Operand);
                }
            }

            return base.VisitUnary(node);
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