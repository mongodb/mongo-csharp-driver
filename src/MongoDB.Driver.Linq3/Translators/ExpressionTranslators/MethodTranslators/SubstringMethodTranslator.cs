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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class SubstringMethodTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var arguments = expression.Arguments;

            if (expression.Method.IsOneOf(StringMethod.Substring, StringMethod.SubstringWithLength))
            {
                var instance = expression.Object;
                var startIndex = arguments[0];
                var length = arguments.Count == 2 ? arguments[1] : null;
                return Helper(instance, startIndex, length, AstTernaryOperator.SubstrCP);
            }

            if (expression.Method.Is(LinqExtensionsMethod.SubstrBytes))
            {
                var instance = arguments[0];
                var startIndex = arguments[1];
                var length = arguments[2];
                return Helper(instance, startIndex, length, AstTernaryOperator.SubstrBytes);
            }

            throw new ExpressionNotSupportedException(expression);

            TranslatedExpression Helper(Expression instance, Expression startIndex, Expression length, AstTernaryOperator substringOperator)
            {
                var translatedInstance = ExpressionTranslator.Translate(context, instance);
                var translatedStartIndex = ExpressionTranslator.Translate(context, startIndex);

                AstExpression translation;
                if (length == null)
                {
                    var lengthOperator = substringOperator == AstTernaryOperator.SubstrCP ? AstUnaryOperator.StrLenCP : AstUnaryOperator.StrLenBytes;
                    if (IsSimple(translatedInstance.Translation) && IsSimple(translatedStartIndex.Translation))
                    {
                        var stringTranslation = translatedInstance.Translation;
                        var indexTranslation = translatedStartIndex.Translation;
                        var lengthTranslation = new AstUnaryExpression(lengthOperator, stringTranslation);
                        var countTranslation = new AstBinaryExpression(AstBinaryOperator.Subtract, lengthTranslation, indexTranslation);

                        translation = new AstTernaryExpression(substringOperator, stringTranslation, indexTranslation, countTranslation);
                    }
                    else
                    {
                        var vars = new[]
                        {
                            new AstComputedField("string", translatedInstance.Translation),
                            new AstComputedField("index", translatedStartIndex.Translation)
                        };
                        var stringField = new AstFieldExpression("$$string");
                        var indexField = new AstFieldExpression("$$index");
                        var lengthTranslation = new AstUnaryExpression(lengthOperator, stringField);
                        var countTranslation = new AstBinaryExpression(AstBinaryOperator.Subtract, lengthTranslation, indexField);
                        var @in = new AstTernaryExpression(substringOperator, stringField, indexField, countTranslation);

                        translation = new AstLetExpression(vars, @in);
                    }
                }
                else
                {
                    var translatedLength = ExpressionTranslator.Translate(context, length);
                    translation = new AstTernaryExpression(substringOperator, translatedInstance.Translation, translatedStartIndex.Translation, translatedLength.Translation);
                }

                var serializer = new StringSerializer();
                return new TranslatedExpression(expression, translation, serializer);
            }
        }

        private static bool IsSimple(AstExpression expression)
        {
            return
                expression.NodeType == AstNodeType.ConstantExpression ||
                expression.NodeType == AstNodeType.FieldExpression;
        }
    }
}
