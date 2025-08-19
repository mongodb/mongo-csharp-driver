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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class ConstantMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(MqlMethod.ConstantWithRepresentation, MqlMethod.ConstantWithSerializer))
            {
                var valueExpression = arguments[0];
                var value = valueExpression.GetConstantValue<object>(expression);

                IBsonSerializer serializer = null;
                if (method.Is(MqlMethod.ConstantWithRepresentation))
                {
                    var representationExpression = arguments[1];
                    var representation = representationExpression.GetConstantValue<BsonType>(expression);
                    var registeredSerializer = context.SerializationDomain.LookupSerializer(valueExpression.Type);
                    if (registeredSerializer is IRepresentationConfigurable representationConfigurableSerializer)
                    {
                        serializer = representationConfigurableSerializer.WithRepresentation(representation);
                    }
                    else
                    {
                        throw new ExpressionNotSupportedException(expression, because: "the registered serializer is not representation configurable");
                    }
                }
                else if (method.Is(MqlMethod.ConstantWithSerializer))
                {
                    var serializerExpression = arguments[1];
                    serializer = serializerExpression.GetConstantValue<IBsonSerializer>(expression);
                }

                if (serializer != null)
                {
                    var serializedValue = SerializationHelper.SerializeValue(serializer, value);
                    var ast = AstExpression.Constant(serializedValue);
                    return new TranslatedExpression(expression, ast, serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
