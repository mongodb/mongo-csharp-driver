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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators
{
    internal static class StringInOrNinMethodToFilterTranslator
    {
        private static readonly MethodInfo[] __stringInOrNinMethods =
        {
            StringMethod.AnyStringInWithEnumerable,
            StringMethod.AnyStringInWithParams,
            StringMethod.AnyStringNinWithEnumerable,
            StringMethod.AnyStringNinWithParams,
            StringMethod.StringInWithEnumerable,
            StringMethod.StringInWithParams,
            StringMethod.StringNinWithEnumerable,
            StringMethod.StringNinWithParams,
        };

        private static readonly MethodInfo[] __stringInMethods =
        {
            StringMethod.AnyStringInWithEnumerable,
            StringMethod.AnyStringInWithParams,
            StringMethod.StringInWithEnumerable,
            StringMethod.StringInWithParams,
        };

        // public static methods
        public static AstFilter Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method  = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__stringInOrNinMethods))
            {
                var fieldExpression = arguments[0];
                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

                var valuesExpression = arguments[1];
                if (valuesExpression is ConstantExpression constantValuesExpression)
                {
                    var serializedValues = new List<BsonValue>();

                    var values = ((IEnumerable<StringOrRegularExpression>)constantValuesExpression.Value).ToList();
                    var stringSerializer = StringSerializer.Instance;
                    var regularExpressionSerializer = BsonRegularExpressionSerializer.Instance;
                    foreach (var value in values)
                    {
                        BsonValue serializedValue;
                        if (value?.Type == typeof(BsonRegularExpression))
                        {
                            var regularExpression = value.RegularExpression;
                            serializedValue = SerializationHelper.SerializeValue(regularExpressionSerializer, regularExpression);
                        }
                        else
                        {
                            var @string = value?.String;
                            serializedValue = SerializationHelper.SerializeValue(stringSerializer, @string);
                        }
                        serializedValues.Add(serializedValue);
                    }

                    return method.IsOneOf(__stringInMethods) ? AstFilter.In(fieldTranslation.AstField, serializedValues) : AstFilter.Nin(fieldTranslation.AstField, serializedValues);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
