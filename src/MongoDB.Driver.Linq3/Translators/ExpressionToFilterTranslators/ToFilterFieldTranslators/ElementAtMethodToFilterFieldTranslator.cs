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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Reflection;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    public static class ElementAtMethodToFilterFieldTranslator
    {
        public static AstFilterField Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(EnumerableMethod.ElementAt))
            {
                var sourceExpression = arguments[0];
                var sourceField = ExpressionToFilterFieldTranslator.Translate(context, sourceExpression);

                var indexExpression = arguments[1];
                if (indexExpression is ConstantExpression indexConstantExpression)
                {
                    var index = (int)indexConstantExpression.Value;

                    if (sourceField.Serializer is IBsonArraySerializer arraySerializer &&
                        arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                    {
                        var itemSerializer = itemSerializationInfo.Serializer;
                        return sourceField.SubField(index.ToString(), itemSerializer);
                    }
                }

            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
