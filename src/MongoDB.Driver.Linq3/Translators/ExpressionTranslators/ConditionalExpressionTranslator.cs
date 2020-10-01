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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class ConditionalExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, ConditionalExpression expression)
        {
            var testExpression = expression.Test;
            var ifTrueExpression = expression.IfTrue;
            var ifFalseExpression = expression.IfFalse;

            var testTranslation = ExpressionTranslator.Translate(context, testExpression);
            var ifTrueTranslation = ExpressionTranslator.Translate(context, ifTrueExpression);
            var ifFalseTranslation = ExpressionTranslator.Translate(context, ifFalseExpression);
            var ast = new AstCondExpression(testTranslation.Ast, ifTrueTranslation.Ast, ifFalseTranslation.Ast);
            var serializer = BsonSerializer.LookupSerializer(expression.Type); // TODO: use known serializer

            return new ExpressionTranslation(expression, ast, serializer);
        }
    }
}
