/* Copyright 2010-present MongoDB Inc.
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
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq
{
    /// <summary>
    /// Exception that is thrown when using a LINQ expression that is not supported.
    /// </summary>
    public class ExpressionNotSupportedException : NotSupportedException
    {
        #region static
        private static string FormatMessage(Expression expression)
        {
            return $"Expression not supported: {expression}.";
        }

        private static string FormatMessage(Expression expression, string because)
        {
            return $"Expression not supported: {expression} because {because}.";
        }

        private static string FormatMessage(Expression expression, Expression containingExpression)
        {
            return $"Expression not supported: {expression} in {containingExpression}.";
        }

        private static string FormatMessage(Expression expression, Expression containingExpression, string because)
        {
            return $"Expression not supported: {expression} in {containingExpression} because {because}.";
        }
        #endregion

        // constructors
        /// <summary>
        /// Initializes an instance of an ExpressionNotSupportedException.
        /// </summary>
        /// <param name="message">The message.</param>
        public ExpressionNotSupportedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes an instance of an ExpressionNotSupportedException.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public ExpressionNotSupportedException(Expression expression)
            : base(FormatMessage(expression))
        {
        }

        /// <summary>
        /// Initializes an instance of an ExpressionNotSupportedException.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="because">The reason.</param>
        public ExpressionNotSupportedException(Expression expression, string because)
            : base(FormatMessage(expression, because))
        {
        }

        /// <summary>
        /// Initializes an instance of an ExpressionNotSupportedException.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="containingExpression">The containing expression.</param>
        public ExpressionNotSupportedException(Expression expression, Expression containingExpression)
            : base(FormatMessage(expression, containingExpression))
        {
        }

        /// <summary>
        /// Initializes an instance of an ExpressionNotSupportedException.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="containingExpression">The containing expression.</param>
        /// <param name="because">The reason.</param>
        public ExpressionNotSupportedException(Expression expression, Expression containingExpression, string because)
            : base(FormatMessage(expression, containingExpression, because))
        {
        }
    }
}
