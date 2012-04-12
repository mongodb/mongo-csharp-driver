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
    /// A class that finds the first parameter in an expression.
    /// </summary>
    public class ExpressionParameterFinder : ExpressionVisitor
    {
        // private fields
        private ParameterExpression _parameter;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ExpressionParameterFinder class.
        /// </summary>
        public ExpressionParameterFinder()
        {
        }

        // public static methods
        /// <summary>
        /// Finds the first parameter in an expression.
        /// </summary>
        /// <param name="node">The expression containing the parameter that should be found.</param>
        /// <returns>The first parameter found in the expression (or null if none was found).</returns>
        public static ParameterExpression FindParameter(Expression node)
        {
            var finder = new ExpressionParameterFinder();
            finder.Visit(node);
            return finder._parameter;
        }

        // protected methods
        /// <summary>
        /// Visits an Expression.
        /// </summary>
        /// <param name="node">The Expression.</param>
        /// <returns>The Expression (posibly modified).</returns>
        protected override Expression Visit(Expression node)
        {
            if (_parameter != null)
            {
                return node; // exit faster if we've already found the parameter
            }
            return base.Visit(node);
        }

        /// <summary>
        /// Remembers this parameter if it is the first parameter found.
        /// </summary>
        /// <param name="node">The ParameterExpression.</param>
        /// <returns>The ParameterExpression.</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_parameter == null)
            {
                _parameter = node;
            }
            return node;
        }
    }
}
