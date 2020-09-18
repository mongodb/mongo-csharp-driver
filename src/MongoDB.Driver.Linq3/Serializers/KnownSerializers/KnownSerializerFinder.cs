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

using System.Linq.Expressions;
using ExpressionVisitor = System.Linq.Expressions.ExpressionVisitor;

namespace MongoDB.Driver.Linq3.Serializers.KnownSerializers
{
    public class KnownSerializerFinder : ExpressionVisitor
    {
        #region static
        // public static methods
        public static KnownSerializersRegistry FindKnownSerializers(Expression root)
        {
            var visitor = new KnownSerializerFinder();
            visitor.Visit(root);
            return visitor._registry;
        }
        #endregion

        // private fields
        private KnownSerializersNode _expressionKnownSerializers = null;
        private readonly KnownSerializersRegistry _registry = new KnownSerializersRegistry();

        // constructors
        public KnownSerializerFinder()
        {
        }

        // public methods
        public override Expression Visit(Expression node)
        {
            _expressionKnownSerializers = new KnownSerializersNode(_expressionKnownSerializers);
            _registry.Add(node, _expressionKnownSerializers);

            var result = base.Visit(node);

            _expressionKnownSerializers = _expressionKnownSerializers.Parent;
            return result;
        }
    }
}
