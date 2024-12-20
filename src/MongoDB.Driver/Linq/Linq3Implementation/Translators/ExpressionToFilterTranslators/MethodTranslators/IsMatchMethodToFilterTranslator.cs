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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class IsMatchMethodToFilterTranslator
    {
        // public static methods
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (RegexMethod.IsMatchMethod(expression, out var fieldExpression, out var regex))
            {
                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
                var regularExpression = new BsonRegularExpression(regex);

                if (fieldTranslation.Serializer is IRepresentationConfigurable representationConfigurable &&
                    representationConfigurable.Representation != BsonType.String)
                {
                    throw new ExpressionNotSupportedException(fieldExpression, expression, because: $"field \"{fieldTranslation.AstField.Path}\" is not represented as a string");
                }

                return AstFilter.Regex(fieldTranslation.AstField, regularExpression.Pattern, regularExpression.Options);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
