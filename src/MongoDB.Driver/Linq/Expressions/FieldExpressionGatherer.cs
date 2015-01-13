/* Copyright 2010-2014 10gen Inc.
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
using System.Linq;
using System.Linq.Expressions;

namespace MongoDB.Driver.Linq.Expressions
{
    internal class FieldExpressionGatherer : MongoExpressionVisitor
    {
        public static IReadOnlyList<FieldExpression> Gather(Expression node, bool includeProjectedFields)
        {
            var gatherer = new FieldExpressionGatherer(includeProjectedFields);
            gatherer.Visit(node);
            return gatherer._fields;
        }

        private List<FieldExpression> _fields;
        private bool _includeProjectedFields;

        private FieldExpressionGatherer(bool includeProjectedFields)
        {
            _fields = new List<FieldExpression>();
            _includeProjectedFields = includeProjectedFields;
        }

        protected override Expression VisitField(FieldExpression node)
        {
            if (!_includeProjectedFields && node.IsProjected)
            {
                Visit(node.Expression);
            }
            else
            {
                _fields.Add(node);
            }
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!IsLinqMethod(node, "Select", "SelectMany"))
            {
                return base.VisitMethodCall(node);
            }

            var source = node.Arguments[0] as FieldExpression;
            if (source != null)
            {
                var fields = FieldExpressionGatherer.Gather(node.Arguments[1], _includeProjectedFields);
                if (fields.Any(x => x.SerializationInfo.ElementName.StartsWith(source.SerializationInfo.ElementName)))
                {
                    _fields.AddRange(fields);
                    return node;
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}