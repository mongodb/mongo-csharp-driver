﻿/* Copyright 2019-present MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Linq2Implementation.Expressions;

namespace MongoDB.Driver.Linq.Linq2Implementation.Processors
{
    /// <summary>
    /// The expression visitor to collect all expression fields which have been defined out of the current scope.
    /// </summary>
    internal sealed class OutOfCurrentScopePrefixCollector : ExtensionExpressionVisitor
    {
        private bool _isWhereSourceConstantExpression = false;
        private readonly HashSet<string> _outOfCurrentScopePrefixCollection = new HashSet<string>();

        /// <summary>
        /// Collects the list of expression fields which have been defined out of the current scope.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>The result collection.</returns>
        public static IEnumerable<string> Collect(Expression expression)
        {
            var outOfScopePrefixCollection = new OutOfCurrentScopePrefixCollector();
            outOfScopePrefixCollection.Visit(expression);
            return outOfScopePrefixCollection._outOfCurrentScopePrefixCollection;
        }

        public override Expression Visit(Expression node)
        {
            var hasOutOfCurrentScopePrefix = node as IHasOutOfCurrentScopePrefix;
            if (hasOutOfCurrentScopePrefix != null &&
                !string.IsNullOrWhiteSpace(hasOutOfCurrentScopePrefix.OutOfCurrentScopePrefix) &&
                !_isWhereSourceConstantExpression)
            {
                _outOfCurrentScopePrefixCollection.Add(hasOutOfCurrentScopePrefix.OutOfCurrentScopePrefix);
            }

            return base.Visit(node);
        }

        protected internal override Expression VisitWhere(WhereExpression node)
        {
            if (node.Source.NodeType == ExpressionType.Constant)
            {
                _isWhereSourceConstantExpression = true;
            }
            var result = base.VisitWhere(node);
            _isWhereSourceConstantExpression = false;
            return result;
        }
    }
}
