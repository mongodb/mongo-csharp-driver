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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public class TranslatedExpression
    {
        // private fields
        private readonly Expression _expression;
        private readonly IBsonSerializer _serializer;
        private readonly AstExpression _translation;

        // constructors
        public TranslatedExpression(Expression expression, AstExpression translation, IBsonSerializer serializer)
        {
            _translation = Throw.IfNull(translation, nameof(translation));
            _expression = Throw.IfNull(expression, nameof(expression));
            _serializer = serializer; // can be null
        }

        // public properties
        public Expression Expression => _expression;

        public IBsonSerializer Serializer => _serializer;

        public AstExpression Translation => _translation;

        // public methods
        public override string ToString()
        {
            return _translation.Render().ToJson();
        }
    }
}
