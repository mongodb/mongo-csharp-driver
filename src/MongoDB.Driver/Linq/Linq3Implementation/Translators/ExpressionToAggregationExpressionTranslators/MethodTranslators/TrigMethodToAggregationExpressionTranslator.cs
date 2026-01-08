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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class TrigMethodToAggregationExpressionTranslator
    {
        private static readonly IReadOnlyMethodInfoSet __binaryTrigMethods;
        private static readonly IReadOnlyMethodInfoSet __unaryTrigMethods;

        static TrigMethodToAggregationExpressionTranslator()
        {
            __binaryTrigMethods = MethodInfoSet.Create(
            [
                MathMethod.Atan2
            ]);

            __unaryTrigMethods = MethodInfoSet.Create(
            [
                MathMethod.Acos,
                MathMethod.Acosh,
                MathMethod.Asin,
                MathMethod.Asinh,
                MathMethod.Atan,
                MathMethod.Atanh,
                MathMethod.Cos,
                MathMethod.Cosh,
                MathMethod.Sin,
                MathMethod.Sinh,
                MathMethod.Tan,
                MathMethod.Tanh,
                MongoDBMathMethod.DegreesToRadians,
                MongoDBMathMethod.RadiansToDegrees
            ]);
        }

        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__unaryTrigMethods))
            {
                var argExpression = arguments.Single();
                var argTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, argExpression);

                var unaryOperator = ToUnaryOperator(method.Name);
                AstExpression ast = AstExpression.Unary(unaryOperator, argTranslation.Ast);

                return new TranslatedExpression(expression, ast, DoubleSerializer.Instance);
            }

            if (method.IsOneOf(__binaryTrigMethods))
            {
                var arg1Expression = arguments[0];
                var arg1Translation = ExpressionToAggregationExpressionTranslator.Translate(context, arg1Expression);

                var arg2Expression = arguments[1];
                var arg2Translation = ExpressionToAggregationExpressionTranslator.Translate(context, arg2Expression);

                var binaryOperator = ToBinaryOperator(method.Name);
                AstExpression ast = AstExpression.Binary(binaryOperator, arg1Translation.Ast, arg2Translation.Ast);

                return new TranslatedExpression(expression, ast, DoubleSerializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstBinaryOperator ToBinaryOperator(string methodName)
        {
            return methodName switch
            {
                "Atan2" => AstBinaryOperator.Atan2,
                _ => throw new ArgumentException($"Unexpected method name: {methodName}.", nameof(methodName))
            };
        }

        private static AstUnaryOperator ToUnaryOperator(string methodName)
        {
            return methodName switch
            {
                "Acos" => AstUnaryOperator.Acos,
                "Acosh" => AstUnaryOperator.Acosh,
                "Asin" => AstUnaryOperator.Asin,
                "Asinh" => AstUnaryOperator.Asinh,
                "Atan" => AstUnaryOperator.Atan,
                "Atanh" => AstUnaryOperator.Atanh,
                "Cos" => AstUnaryOperator.Cos,
                "Cosh" => AstUnaryOperator.Cosh,
                "DegreesToRadians" => AstUnaryOperator.DegreesToRadians,
                "RadiansToDegrees" => AstUnaryOperator.RadiansToDegrees,
                "Sin" => AstUnaryOperator.Sin,
                "Sinh" => AstUnaryOperator.Sinh,
                "Tan" => AstUnaryOperator.Tan,
                "Tanh" => AstUnaryOperator.Tanh,
                _ => throw new ArgumentException($"Unexpected method name: {methodName}.", nameof(methodName))
            };
        }
    }
}
