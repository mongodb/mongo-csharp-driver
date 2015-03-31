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
    internal class FieldGatherer : MongoExpressionVisitor
    {
        public static IReadOnlyList<SerializationExpression> Gather(Expression node)
        {
            var gatherer = new FieldGatherer();
            gatherer.Visit(node);
            return gatherer._serializationExpressions;
        }

        private List<SerializationExpression> _serializationExpressions;

        private FieldGatherer()
        {
            _serializationExpressions = new List<SerializationExpression>();
        }

        protected override Expression VisitSerialization(SerializationExpression node)
        {
            if (node.SerializationInfo.ElementName != null)
            {
                _serializationExpressions.Add(node);
            }
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!IsLinqMethod(node, "Select", "SelectMany"))
            {
                return base.VisitMethodCall(node);
            }

            var source = node.Arguments[0] as SerializationExpression;
            if (source != null)
            {
                var fields = FieldGatherer.Gather(node.Arguments[1]);
                if (fields.Any(x => x.SerializationInfo.ElementName.StartsWith(source.SerializationInfo.ElementName)))
                {
                    _serializationExpressions.AddRange(fields);
                    return node;
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}