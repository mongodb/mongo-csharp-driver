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
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    public static class EndsWithMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(StringMethod.EndsWith))
            {
                var objectExpression = expression.Object;
                var field = ExpressionToFilterFieldTranslator.Translate(context, objectExpression);

                var valueExpression = arguments[0];
                if (valueExpression is ConstantExpression valueConstantExpression)
                {
                    var value = (string)valueConstantExpression.Value;
                    var pattern = Regex.Escape(value) + "$";
                    return new AstRegexFilter(field, pattern, options: "");
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
