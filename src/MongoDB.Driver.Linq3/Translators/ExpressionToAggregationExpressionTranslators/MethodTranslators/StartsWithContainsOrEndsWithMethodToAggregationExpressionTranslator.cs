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
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class StartsWithContainsOrEndsWithMethodToAggregationExpressionTranslator
    {
        private static MethodInfo[] __startsWithContainsOrEndWithMethods;
        private static MethodInfo[] __withComparisonTypeMethods;
        private static MethodInfo[] __withIgnoreCaseAndCultureMethods;

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
                    stringAst = ToLower(stringAst);
                    substringAst = ToLower(stringAst);
                }

                var ast = CreateAst(method.Name, stringAst, substringAst);
                return new AggregationExpression(expression, ast, new BooleanSerializer());
            }

            throw new ExpressionNotSupportedException(expression);

            AstExpression CreateAst(string methodName, AstExpression stringAst, AstExpression substringAst)
            {
                return methodName switch
                {
                    "StartsWith" => CreateStartsWithAst(),
                    "Contains" => CreateContainsAst(),
                    "EndsWith" => CreateEndsWithAst(),
                    _ => throw new InvalidOperationException()
                };

                AstExpression CreateStartsWithAst()
                {
                    return AstExpression.Eq(AstExpression.IndexOfCP(stringAst, substringAst), 0);
                }

                AstExpression CreateContainsAst()
                {
                    return AstExpression.Gte(AstExpression.IndexOfCP(stringAst, substringAst), 0);
                }

                AstExpression CreateEndsWithAst()
                {
                    var vars = new List<AstComputedField>();
                    var stringSimpleAst = CreateSimpleAst(stringAst, vars, "string");
                    var substringSimpleAst = CreateSimpleAst(substringAst, vars, "substring");
                    var startAst = AstExpression.Subtract(
                        CreateStrlenCPAst(stringSimpleAst),
                        CreateStrlenCPAst(substringSimpleAst));
                        
                    var ast = AstExpression.Gte(AstExpression.IndexOfCP(stringSimpleAst, substringSimpleAst, startAst), 0);

                    if (vars.Count == 0)
                    {
                        return ast;
                    }
                    else
                    {
                        return AstExpression.Let(vars, ast);
                    }
                }

                AstExpression CreateSimpleAst(AstExpression ast, List<AstComputedField> vars, string name)
                {
                    if (ast.NodeType == AstNodeType.ConstantExpression || ast.NodeType == AstNodeType.FieldExpression)
                    {
                        return ast;
                    }

                    vars.Add(new AstComputedField(name, ast));
                    return AstExpression.Field("$" + name);
                }

                AstExpression CreateStrlenCPAst(AstExpression valueAst)
                {
                    if (valueAst is AstConstantExpression valueConstantAst)
                    {
                        var value = (string)valueConstantAst.Value;
                        return value.Length;
                    }
                    else
                    {
                        return AstExpression.StrLenCP(valueAst);
                    }
                            
                }
            }

            bool GetIgnoreCaseFromComparisonType(Expression comparisonTypeExpression)
            {
                if (comparisonTypeExpression is ConstantExpression comparisonTypeConstantExpression)
                {
                    var comparisonType = (StringComparison)comparisonTypeConstantExpression.Value;
                    switch (comparisonType)
                    {
                        case StringComparison.CurrentCulture: return false;
                        case StringComparison.CurrentCultureIgnoreCase: return true;
                    }
                }

                throw new ExpressionNotSupportedException(expression);
            }

            bool GetIgnoreCaseFromIgnoreCaseAndCulture(Expression ignoreCaseExpression, Expression cultureExpression)
            {
                if (ignoreCaseExpression is ConstantExpression ignoreCaseConstantExpression &&
                    cultureExpression is ConstantExpression cultureConstantExpression)
                {
                    var ignoreCase = (bool)ignoreCaseConstantExpression.Value;
                    var culture = (CultureInfo)cultureConstantExpression.Value;

                    if (culture == CultureInfo.CurrentCulture)
                    {
                        return ignoreCase;
                    }
                }

                throw new ExpressionNotSupportedException(expression);
            }

            AstExpression ToLower(AstExpression ast)
            {
                if (ast is AstConstantExpression astConstant)
                {
                    var value = astConstant.Value.AsString;
                    return value.ToLower();
                }
                else
                {
                    return AstExpression.ToLower(ast);
                }
            }
        }
    }
}
