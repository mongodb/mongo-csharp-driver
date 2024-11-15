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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class MethodCallExpressionToFilterFieldTranslator
    {
        public static AstFilterField Translate(TranslationContext context, MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "AllElements": return AllElementsMethodToFilterFieldTranslator.Translate(context, expression);
                case "AllMatchingElements": return AllMatchingElementsMethodToFilterFieldTranslator.Translate(context, expression);
                case "ElementAt": return ElementAtMethodToFilterFieldTranslator.Translate(context, expression);
                case "Field": return FieldMethodToFilterFieldTranslator.Translate(context, expression);
                case "First": return FirstMethodToFilterFieldTranslator.Translate(context, expression);
                case "FirstMatchingElement": return FirstMatchingElementMethodToFilterFieldTranslator.Translate(context, expression);
                case "get_Item": return GetItemMethodToFilterFieldTranslator.Translate(context, expression);
                case "Select": return SelectMethodToFilterFieldTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
