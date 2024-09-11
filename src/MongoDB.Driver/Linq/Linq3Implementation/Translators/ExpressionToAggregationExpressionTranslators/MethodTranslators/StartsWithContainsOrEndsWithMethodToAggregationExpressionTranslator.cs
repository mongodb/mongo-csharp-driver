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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __startsWithContainsOrEndsWithMethods;
        private static readonly MethodInfo[] __withComparisonTypeMethods;
        private static readonly MethodInfo[] __withIgnoreCaseAndCultureMethods;

        static StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator()
        {
            __startsWithContainsOrEndsWithMethods = new[]
            {
                StringMethod.StartsWithWithChar,
                StringMethod.StartsWithWithString,
                StringMethod.StartsWithWithStringAndComparisonType,
                StringMethod.StartsWithWithStringAndIgnoreCaseAndCulture,
                StringMethod.ContainsWithChar,
                StringMethod.ContainsWithCharAndComparisonType,
                StringMethod.ContainsWithString,
                StringMethod.ContainsWithStringAndComparisonType,
                StringMethod.EndsWithWithChar,
                StringMethod.EndsWithWithString,
                StringMethod.EndsWithWithStringAndComparisonType,
                StringMethod.EndsWithWithStringAndIgnoreCaseAndCulture
            };

            __withComparisonTypeMethods = new[]
            {
                StringMethod.StartsWithWithStringAndComparisonType,
                StringMethod.ContainsWithCharAndComparisonType,
                StringMethod.ContainsWithStringAndComparisonType,
                StringMethod.EndsWithWithStringAndComparisonType
            };

            __withIgnoreCaseAndCultureMethods = new[]
            {
                StringMethod.StartsWithWithStringAndIgnoreCaseAndCulture,
                StringMethod.EndsWithWithStringAndIgnoreCaseAndCulture
            };
        }

        public static bool CanTranslate(MethodCallExpression expression)
        {
            var method = expression.Method;

            if (method.IsOneOf(__startsWithContainsOrEndsWithMethods))
            {
                return true;
            }

            // on .NET Framework string.Contains(char) compiles to Enumerable.Contains<char>(string, char)
            // on all frameworks we will translate Enumerable.Contains<char>(string, char) the same as string.Contains(char)
            if (method.Is(EnumerableMethod.Contains) && expression.Arguments[0].Type == typeof(string))
            {
                return true;
            }

            return false;
        }

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (CanTranslate(expression))
            {
                Expression objectExpression;
                if (method.Is(EnumerableMethod.Contains))
                {
                    objectExpression = arguments[0];
                    arguments = new ReadOnlyCollection<Expression>(arguments.Skip(1).ToList());

                    if (objectExpression.Type != typeof(string))
                    {
                        throw new ExpressionNotSupportedException(objectExpression, expression, because: "type implementing IEnumerable<char> is not string");
                    }
                }
                else
                {
                    objectExpression = expression.Object;
                }

                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);
                var valueExpression = arguments[0];
                AggregationExpression valueTranslation;
                if (valueExpression.Type == typeof(char) &&
                    valueExpression is ConstantExpression constantValueExpression)
                {
                    var c = (char)constantValueExpression.Value;
                    var value = new string(c, 1);
                    valueTranslation = new AggregationExpression(valueExpression, value, objectTranslation.Serializer);
                }
                else
                {
                    valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                    if (valueTranslation.Serializer is IRepresentationConfigurable representationConfigurable &&
                        representationConfigurable.Representation != BsonType.String)
                    {
                        throw new ExpressionNotSupportedException(valueExpression, expression, because: "it is not serialized as a string");
                    }
                }
                bool ignoreCase = false;
                if (IsWithComparisonTypeMethod(method))
                {
                    var comparisonTypeExpression = arguments[1];
                    ignoreCase = GetIgnoreCaseFromComparisonType(comparisonTypeExpression);
                }
                if (IsWithIgnoreCaseAndCultureMethod(method))
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
                    substringAst = AstExpression.ToLower(substringAst);
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
                    var startVar = AstExpression.Var("start");
                    var ast = AstExpression.Let(
                        var: AstExpression.VarBinding(startVar, startAst),
                        @in: AstExpression.And(
                            AstExpression.Gte(startVar, 0),
                            AstExpression.Eq(AstExpression.IndexOfCP(stringSimpleAst, substringSimpleAst, startVar), startVar)));
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

                throw new ExpressionNotSupportedException(comparisonTypeExpression, expression, because: $"{comparisonType} is not supported");
            }

            bool GetIgnoreCaseFromIgnoreCaseAndCulture(Expression ignoreCaseExpression, Expression cultureExpression)
            {
                var ignoreCase = ignoreCaseExpression.GetConstantValue<bool>(containingExpression: expression);
                var culture = cultureExpression.GetConstantValue<CultureInfo>(containingExpression: expression);

                if (culture != CultureInfo.CurrentCulture)
                {
                    throw new ExpressionNotSupportedException(cultureExpression, expression, because: "the supplied culture is not the current culture");
                }

                return ignoreCase;
            }

            bool IsWithComparisonTypeMethod(MethodInfo method)
            {
                if (method.IsOneOf(__withComparisonTypeMethods))
                {
                    return true;
                }

                return false;
            }

            bool IsWithIgnoreCaseAndCultureMethod(MethodInfo method)
            {
                if (method.IsOneOf(__withIgnoreCaseAndCultureMethods))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
