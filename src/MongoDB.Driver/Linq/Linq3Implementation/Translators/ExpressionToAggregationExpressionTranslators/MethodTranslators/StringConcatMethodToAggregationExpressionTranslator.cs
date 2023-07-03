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

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class StringConcatMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __stringConcatMethods = new[]
        {
            StringMethod.ConcatWith2Strings,
            StringMethod.ConcatWith3Strings,
            StringMethod.ConcatWith4Strings,
            StringMethod.ConcatWithStringArray
        };

        public static bool CanTranslate(MethodCallExpression expression)
            => expression.Method.IsOneOf(__stringConcatMethods);

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            IEnumerable<AstExpression> argumentsTranslations = null;

            if (method.IsOneOf(
                StringMethod.ConcatWith2Strings,
                StringMethod.ConcatWith3Strings,
                StringMethod.ConcatWith4Strings))
            {
                argumentsTranslations =
                    arguments.Select(a => ExpressionToAggregationExpressionTranslator.Translate(context, a).Ast);
            }

            if (method.Is(StringMethod.ConcatWithStringArray))
            {
                var argumentTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, arguments.Single());
                if (argumentTranslation.Ast is AstComputedArrayExpression astArray)
                {
                    argumentsTranslations = astArray.Items;
                }
            }

            if (argumentsTranslations != null)
            {
                var ast = AstExpression.Concat(argumentsTranslations.ToArray());
                return new AggregationExpression(expression, ast, StringSerializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
