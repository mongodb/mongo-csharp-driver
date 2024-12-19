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
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class IsMissingMethodToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(MqlMethod.IsMissing, MqlMethod.IsNullOrMissing))
            {
                var fieldExpression = arguments[0];
                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

                return method switch
                {
                    _ when method.Is(MqlMethod.IsMissing) => AstFilter.NotExists(fieldTranslation.AstField),
                    _ when method.Is(MqlMethod.IsNullOrMissing) => AstFilter.Eq(fieldTranslation.AstField, BsonNull.Value), // matches missing fields also
                    _ => throw new ExpressionNotSupportedException(expression)
                };
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
