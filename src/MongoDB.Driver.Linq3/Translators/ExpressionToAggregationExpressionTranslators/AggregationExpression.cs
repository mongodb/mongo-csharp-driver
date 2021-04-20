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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators
{
    internal class AggregationExpression
    {
        // private fields
        private readonly AstExpression _ast;
        private readonly Expression _expression;
        private readonly IBsonSerializer _serializer;

        // constructors
        public AggregationExpression(Expression expression, AstExpression ast, IBsonSerializer serializer)
        {
            _expression = Ensure.IsNotNull(expression, nameof(expression));
            _ast = Ensure.IsNotNull(ast, nameof(ast));
            _serializer = serializer; // can be null
        }

        // public properties
        public AstExpression Ast => _ast;
        public Expression Expression => _expression;
        public IBsonSerializer Serializer => _serializer;

        // public methods
        public override string ToString()
        {
            return _ast.Render().ToJson();
        }
    }
}
