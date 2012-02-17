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

using MongoDB.Bson;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// A class that replaces all occurences of one parameter with a different parameter.
    /// </summary>
    public class ExpressionParameterReplacer : ExpressionVisitor
    {
        // private fields
        private ParameterExpression _fromParameter;
        private ParameterExpression _toParameter;

        // constructors
        /// <summary>
        /// Initializes a new instance of the ExpressionParameterReplacer class.
        /// </summary>
        public ExpressionParameterReplacer(ParameterExpression fromParameter, ParameterExpression toParameter)
        {
            _fromParameter = fromParameter;
            _toParameter = toParameter;
        }

        // public methods
        /// <summary>
        /// Replaces all occurences of one parameter with a different parameter.
        /// </summary>
        /// <param name="node">The expression containing the parameter that should be replaced.</param>
        /// <param name="fromParameter">The from parameter.</param>
        /// <param name="toParameter">The to parameter.</param>
        /// <returns>The expression with all occurrences of the parameter replaced.</returns>
        public static Expression ReplaceParameter(Expression node, ParameterExpression fromParameter, ParameterExpression toParameter)
        {
            var replacer = new ExpressionParameterReplacer(fromParameter, toParameter);
            return replacer.Visit(node);
        }

        /// <summary>
        /// Replaces the from parameter with the two parameter if it maches.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The parameter (replaced if it matched).</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _fromParameter)
            {
                return _toParameter;
            }
            return node;
        }
    }
}
