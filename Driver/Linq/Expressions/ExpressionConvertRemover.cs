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
    /// A class that removes unnecessary conversions.
    /// </summary>
    public class ExpressionConvertRemover : ExpressionVisitor
    {
        // private fields
        private Expression _expression;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ExpressionConvertRemover class.
        /// </summary>
        /// <param name="expression">The expression to be validated.</param>
        public ExpressionConvertRemover(Expression expression)
        {
            _expression = expression;
        }

        // public methods
        /// <summary>
        /// Replaces all occurences of one parameter with a different parameter.
        /// </summary>
        /// <param name="node">The expression containing the parameter that should be replaced.</param>
        /// <param name="fromParameter">The from parameter.</param>
        /// <param name="toExpression">The expression that replaces the parameter.</param>
        /// <returns>The expression with all occurrences of the parameter replaced.</returns>
        public static Expression RemoveConversions(Expression node)
        {
            var remover = new ExpressionConvertRemover(node);
            return remover.Visit(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Convert)
            {
                return base.VisitUnary(node);
            }

            if (node.Type.IsAssignableFrom(node.Operand.Type))
            {
                return Visit(node.Operand);
            }

            return base.VisitUnary(node);
        }
    }
}