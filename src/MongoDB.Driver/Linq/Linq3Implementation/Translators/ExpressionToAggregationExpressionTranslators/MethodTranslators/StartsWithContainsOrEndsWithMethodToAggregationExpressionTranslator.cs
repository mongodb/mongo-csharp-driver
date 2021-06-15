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
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __startsWithContainsOrEndWithMethods;
        private static readonly MethodInfo[] __withComparisonTypeMethods;
        private static readonly MethodInfo[] __withIgnoreCaseAndCultureMethods;

        static StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator()
        {
            __startsWithContainsOrEndWithMethods = new[]
            {
                StringMethod.StartsWith,
                StringMethod.StartsWithWithComparisonType,
                StringMethod.StartsWithWithIgnoreCaseAndCulture,
                StringMethod.Contains,
                StringMethod.EndsWith,
                StringMethod.EndsWithWithComparisonType,
                StringMethod.EndsWithWithIgnoreCaseAndCulture
            };

            __withComparisonTypeMethods = new[]
            {
                StringMethod.StartsWithWithComparisonType,
                StringMethod.EndsWithWithComparisonType
            };

            __withIgnoreCaseAndCultureMethods = new[]
            {
                StringMethod.StartsWithWithIgnoreCaseAndCulture,
                StringMethod.EndsWithWithIgnoreCaseAndCulture
            };
        }

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__startsWithContainsOrEndWithMethods))
            {
                var objectExpression = expression.Object;
                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
                var valueExpression = arguments[0];
                var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                bool ignoreCase = false;
                if (method.IsOneOf(__withComparisonTypeMethods))
                {
                    var comparisonTypeExpression = arguments[1];
                    ignoreCase = GetIgnoreCaseFromComparisonType(comparisonTypeExpression);
                }
                if (method.IsOneOf(__withIgnoreCaseAndCultureMethods))
                {
                    var ignoreCaseExpression = arguments[1];
                    var cultureExpression = arguments[2];
                    ignoreCase = GetIgnoreCaseFromIgnoreCaseAndCulture(ignoreCaseExpression, cultureExpression);
                }
                var stringAst = objectTranslation.Ast;
                var substringAst = valueTranslation.Ast;
                if (ignoreCase)
                {
                    stringAst = AstExpression.ToLower(stringAst);
                    substringAst = AstExpression.ToLower(stringAst);
                }
                var ast = CreateAst(method.Name, stringAst, substringAst);
                return new AggregationExpression(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);

            static AstExpression CreateAst(string methodName, AstExpression stringAst, AstExpression substringAst)
            {
                return methodName switch
                {
                    "StartsWith" => CreateStartsWithAst(stringAst, substringAst),
                    "Contains" => CreateContainsAst(stringAst, substringAst),
                    "EndsWith" => CreateEndsWithAst(stringAst, substringAst),
                    _ => throw new InvalidOperationException()
                };

                static AstExpression CreateStartsWithAst(AstExpression stringAst, AstExpression substringAst)
                {
                    return AstExpression.Eq(AstExpression.IndexOfCP(stringAst, substringAst), 0);
                }

                static AstExpression CreateContainsAst(AstExpression stringAst, AstExpression substringAst)
                {
                    return AstExpression.Gte(AstExpression.IndexOfCP(stringAst, substringAst), 0);
                }

                static AstExpression CreateEndsWithAst(AstExpression stringAst, AstExpression substringAst)
                {
                    var (stringVar, stringSimpleAst) = AstExpression.UseVarIfNotSimple("string", stringAst);
                    var (substringVar, substringSimpleAst) = AstExpression.UseVarIfNotSimple("substring", substringAst);
                    var startAst = AstExpression.Subtract(AstExpression.StrLenCP(stringSimpleAst), AstExpression.StrLenCP(substringSimpleAst));                      
                    var ast = AstExpression.Gte(AstExpression.IndexOfCP(stringSimpleAst, substringSimpleAst, startAst), 0);
                    return AstExpression.Let(stringVar, substringVar, ast);
                }
            }

            bool GetIgnoreCaseFromComparisonType(Expression comparisonTypeExpression)
            {
                var comparisonType = comparisonTypeExpression.GetConstantValue<StringComparison>(containingExpression: expression);
                switch (comparisonType)
                {
                    case StringComparison.CurrentCulture: return false;
                    case StringComparison.CurrentCultureIgnoreCase: return true;
                }

                throw new ExpressionNotSupportedException(comparisonTypeExpression, expression);
            }

            bool GetIgnoreCaseFromIgnoreCaseAndCulture(Expression ignoreCaseExpression, Expression cultureExpression)
            {
                var ignoreCase = ignoreCaseExpression.GetConstantValue<bool>(containingExpression: expression);
                var culture = cultureExpression.GetConstantValue<CultureInfo>(containingExpression: expression);

                if (culture == CultureInfo.CurrentCulture)
                {
                    return ignoreCase;
                }

                throw new ExpressionNotSupportedException(cultureExpression, expression);
            }
        }
    }
}
