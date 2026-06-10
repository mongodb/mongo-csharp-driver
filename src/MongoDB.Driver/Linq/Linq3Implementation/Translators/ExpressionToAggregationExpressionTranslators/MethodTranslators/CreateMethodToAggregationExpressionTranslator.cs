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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class CreateMethodToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(TupleOrValueTupleMethod.CreateOverloads))
            {
                var tupleType = method.ReturnType;

                var items = new AstExpression[arguments.Count];
                var itemSerializers = new IBsonSerializer[arguments.Count];
                for (var i = 0; i < arguments.Count; i++)
                {
                    var valueExpression = arguments[i];
                    var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                    AstExpression item;
                    IBsonSerializer itemSerializer;
                    if (i < 7)
                    {
                        item = valueTranslation.Ast;
                        itemSerializer = valueTranslation.Serializer;
                    }
                    else
                    {
                        item = AstExpression.ComputedArray(valueTranslation.Ast);
                        itemSerializer = CreateTupleSerializer(tupleType, new[] { valueTranslation.Serializer });
                    }
                    items[i] = item;
                    itemSerializers[i] = itemSerializer;
                }

                var ast = AstExpression.ComputedArray(items);
                var tupleSerializer = CreateTupleSerializer(tupleType, itemSerializers);
                return new TranslatedExpression(expression, ast, tupleSerializer);
            }

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (method.Is(KeyValuePairMethod.Create))
            {
                var keyExpression = arguments[0];
                var valueExpression = arguments[1];
                return NewKeyValuePairExpressionToAggregationExpressionTranslator.Translate(context, expression, keyExpression, valueExpression);
            }
#endif
            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonSerializer CreateTupleSerializer(Type tupleType, IEnumerable<IBsonSerializer> itemSerializers)
        {
            return tupleType.IsTuple() ? TupleSerializer.Create(itemSerializers) : ValueTupleSerializer.Create(itemSerializers);
        }
    }
}
