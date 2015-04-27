﻿/* Copyright 2010-2014 MongoDB Inc.
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

using System.Linq.Expressions;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors
{
    internal class ParameterReplacer : ExtensionExpressionVisitor
    {
        public static Expression Replace(Expression target, ParameterExpression parameter, Expression replacement)
        {
            return new ParameterReplacer(parameter, replacement).Visit(target);
        }

        private readonly ParameterExpression _parameter;
        private readonly Expression _replacement;

        private ParameterReplacer(ParameterExpression parameter, Expression replacement)
        {
            _parameter = parameter;
            _replacement = replacement;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_parameter == node)
            {
                return _replacement;
            }

            return base.Visit(node);
        }
    }
}
