﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class IsNullOrEmptyMethodTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(StringMethod.IsNullOrEmpty))
            {
                var stringExpression = arguments[0];

                var stringTranslation = ExpressionTranslator.Translate(context, stringExpression);
                AstExpression ast;
                if (IsSimple(stringTranslation.Ast))
                {
                    ast = new AstOrExpression(
                        new AstBinaryExpression(AstBinaryOperator.Eq, stringTranslation.Ast, BsonNull.Value),
                        new AstBinaryExpression(AstBinaryOperator.Eq, stringTranslation.Ast, ""));
                }
                else
                {
                    ast = new AstLetExpression(
                        vars: new[] { new AstComputedField("this", stringTranslation.Ast) },
                        @in: new AstOrExpression(
                            new AstBinaryExpression(AstBinaryOperator.Eq, new AstFieldExpression("$$this"), BsonNull.Value),
                            new AstBinaryExpression(AstBinaryOperator.Eq, new AstFieldExpression("$$this"), "")));
                }

                return new ExpressionTranslation(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsSimple(AstExpression expression)
        {
            return
                expression.NodeType == AstNodeType.ConstantExpression ||
                expression.NodeType == AstNodeType.FieldExpression;
        }
    }
}
