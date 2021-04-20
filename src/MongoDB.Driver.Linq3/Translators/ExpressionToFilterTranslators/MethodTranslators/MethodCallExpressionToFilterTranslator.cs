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
using MongoDB.Driver.Linq3.Ast.Filters;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class MethodCallExpressionToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "Contains": return ContainsMethodToFilterTranslator.Translate(context, expression);
                case "EndsWith": return EndsWithMethodToFilterTranslator.Translate(context, expression);
                case "Equals": return EqualsMethodToFilterTranslator.Translate(context, expression);
                case "HasFlag": return HasFlagMethodToFilterTranslator.Translate(context, expression);
                case "IsMatch": return IsMatchMethodToFilterTranslator.Translate(context, expression);
                case "IsNullOrEmpty": return IsNullOrEmptyMethodToFilterTranslator.Translate(context, expression);
                case "StartsWith": return StartsWithMethodToFilterTranslator.Translate(context, expression);

                case "All":
                case "Any":
                    return AllOrAnyMethodToFilterTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
