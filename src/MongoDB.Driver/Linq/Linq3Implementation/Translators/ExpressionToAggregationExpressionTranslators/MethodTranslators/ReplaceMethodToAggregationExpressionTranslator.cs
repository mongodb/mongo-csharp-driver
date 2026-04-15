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
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class ReplaceMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            AstExpression inputAst = null;
            AstExpression findAst = null;
            AstExpression replacementAst = null;
            if (method.Is(StringMethod.ReplaceWithString))
            {
                inputAst = ExpressionToAggregationExpressionTranslator.Translate(context, expression.Object).Ast;
                findAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[0]).Ast;
                replacementAst = ExpressionToAggregationExpressionTranslator.Translate(context, arguments[1]).Ast;
            }
            else if (method.Is(StringMethod.ReplaceWithChars))
            {
                inputAst = ExpressionToAggregationExpressionTranslator.Translate(context, expression.Object).Ast;
                var findChar = arguments[0].GetConstantValue<char>(expression);
                findAst = AstExpression.Constant(new string(findChar, 1));

                var replacementChar = arguments[1].GetConstantValue<char>(expression);
                replacementAst = AstExpression.Constant(new string(replacementChar, 1));
            }
            else if (method.IsOneOf(RegexMethod.ReplaceOverloads))
            {
                Expression inputExpression = null;
                Expression replacementExpression = null;

                if (method == RegexMethod.Replace)
                {
                    var findTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, expression.Object);
                    if (SerializationHelper.GetRepresentation(findTranslation.Serializer) != BsonType.RegularExpression)
                    {
                        throw new ExpressionNotSupportedException(expression);
                    }

                    findAst = findTranslation.Ast;
                    inputExpression = arguments[0];
                    replacementExpression = arguments[1];
                }
                else if (method == RegexMethod.StaticReplace || method == RegexMethod.StaticReplaceWithOptions)
                {
                    var options = arguments.Count == 4 ? arguments[3].GetConstantValue<RegexOptions>(expression) : RegexOptions.None;
                    var pattern = arguments[1].GetConstantValue<string>(expression);
                    findAst = new BsonRegularExpression(new Regex(pattern, options));
                    inputExpression = arguments[0];
                    replacementExpression = arguments[2];
                }

                inputAst = ExpressionToAggregationExpressionTranslator.Translate(context, inputExpression).Ast;
                replacementAst = ExpressionToAggregationExpressionTranslator.Translate(context, replacementExpression).Ast;
            }

            if (inputAst != null && findAst != null && replacementAst != null)
            {
                var serializer = context.GetSerializer(expression);
                var ast = AstExpression.ReplaceAll(inputAst, findAst, replacementAst);
                return new TranslatedExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
