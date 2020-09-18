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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodCallTranslators
{
    public static class ContainsTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Contains))
            {
                var source = expression.Arguments[0];
                var value = expression.Arguments[1];

                var translatedSource = ExpressionTranslator.Translate(context, source);
                var translatedValue = ExpressionTranslator.Translate(context, value);

                //var translation = new BsonDocument("$in", new BsonArray { translatedValue.Translation, translatedSource.Translation });
                var translation = new AstBinaryExpression(AstBinaryOperator.In, translatedValue.Translation, translatedSource.Translation);
                return new TranslatedExpression(expression, translation, null);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
