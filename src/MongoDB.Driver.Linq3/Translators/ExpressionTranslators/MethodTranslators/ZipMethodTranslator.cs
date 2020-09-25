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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class ZipMethodTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Zip))
            {
                var arguments = expression.Arguments;
                var first = arguments[0];
                var second = arguments[1];
                var resultSelector = (LambdaExpression)arguments[2];

                var translatedFirst = ExpressionTranslator.Translate(context, first);
                var translatedSecond = ExpressionTranslator.Translate(context, second);

                var resultSelectorParameters = resultSelector.Parameters;
                var parameter1 = resultSelectorParameters[0];
                var parameter2 = resultSelectorParameters[1];
                var symbol1 = new Symbol("$" + parameter1.Name, BsonSerializer.LookupSerializer(parameter1.Type));
                var symbol2 = new Symbol("$" + parameter2.Name, BsonSerializer.LookupSerializer(parameter2.Type));
                var resultSelectorContext = context.WithSymbols((parameter1, symbol1), (parameter2, symbol2));
                var translatedSelector = ExpressionTranslator.Translate(resultSelectorContext, resultSelector.Body);

                var translation = new AstMapExpression(
                    input: new AstZipExpression(new[] { translatedFirst.Translation, translatedSecond.Translation }),
                    @as: "z__",
                    @in: new AstLetExpression(
                        vars: new[]
                        {
                            new AstComputedField(parameter1.Name, new AstBinaryExpression(AstBinaryOperator.ArrayElemAt, new AstFieldExpression("$$z__"), 0)),
                            new AstComputedField(parameter2.Name, new AstBinaryExpression(AstBinaryOperator.ArrayElemAt, new AstFieldExpression("$$z__"), 1))
                        },
                        @in: translatedSelector.Translation));

                var serializer = IEnumerableSerializer.Create(translatedSelector.Serializer);
                return new TranslatedExpression(expression, translation, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
