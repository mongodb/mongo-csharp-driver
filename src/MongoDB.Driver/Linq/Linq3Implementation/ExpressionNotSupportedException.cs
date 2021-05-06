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

namespace MongoDB.Driver.Linq.Linq3Implementation
{
    internal class ExpressionNotSupportedException : NotSupportedException
    {
        #region static
        private static string FormatMessage(Expression expression)
        {
            return $"Expression not supported: {expression}.";
        }

        private static string FormatMessage(Expression expression, Expression containingExpression)
        {
            return $"Expression not supported: {expression} in {containingExpression}.";
        }
        #endregion

        // constructors
        public ExpressionNotSupportedException(string message)
            : base(message)
        {
        }

        public ExpressionNotSupportedException(Expression expression)
            : base(FormatMessage(expression))
        {
        }

        public ExpressionNotSupportedException(Expression expression, Expression containingExpression)
            : base(FormatMessage(expression, containingExpression))
        {
        }
    }
}
