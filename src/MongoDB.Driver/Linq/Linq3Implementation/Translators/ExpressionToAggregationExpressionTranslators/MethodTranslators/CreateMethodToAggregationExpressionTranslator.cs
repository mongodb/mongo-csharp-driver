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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class CreateMethodToAggregationExpressionTranslator
    {
        private static MethodInfo[] __tupleCreateMethods = new[]
        {
            TupleMethod.Create1,
            TupleMethod.Create2,
            TupleMethod.Create3,
            TupleMethod.Create4,
            TupleMethod.Create5,
            TupleMethod.Create6,
            TupleMethod.Create7,
            TupleMethod.Create8
        };

        private static MethodInfo[] __valueTupleCreateMethods = new[]
        {
            ValueTupleMethod.Create1,
            ValueTupleMethod.Create2,
            ValueTupleMethod.Create3,
            ValueTupleMethod.Create4,
            ValueTupleMethod.Create5,
            ValueTupleMethod.Create6,
            ValueTupleMethod.Create7,
            ValueTupleMethod.Create8
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression, IBsonSerializer targetSerializer)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__tupleCreateMethods) || method.IsOneOf(__valueTupleCreateMethods))
            {
                var tupleType = method.ReturnType;
                var isValueTuple = tupleType.IsValueTuple();

                IBsonTupleSerializer tupleTargetSerializer = null;
                if (targetSerializer != null && (tupleTargetSerializer = targetSerializer as IBsonTupleSerializer) == null)
                {
                    throw new ExpressionNotSupportedException(expression, because: $"serializer {targetSerializer.GetType()} does not implement IBsonTupleSerializer");
                }

                var items = new AstExpression[arguments.Count];
                var itemSerializers = new IBsonSerializer[arguments.Count];
                for (var i = 0; i < arguments.Count; i++)
                {
                    var itemExpression = arguments[i];
                    var itemTargetSerializer = tupleTargetSerializer?.GetItemSerializer(i);
                    var itemTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, itemExpression, itemTargetSerializer);
                    AstExpression item;
                    IBsonSerializer itemSerializer;
                    if (i < 7)
                    {
                        item = itemTranslation.Ast;
                        itemSerializer = itemTranslation.Serializer;
                    }
                    else
                    {
                        item = AstExpression.ComputedArray(itemTranslation.Ast);
                        itemSerializer = CreateTupleSerializer(isValueTuple, [itemTranslation.Serializer]);
                    }
                    items[i] = item;
                    itemSerializers[i] = itemSerializer;
                }
                var ast = AstExpression.ComputedArray(items);

                var resultSerializer = targetSerializer ?? CreateTupleSerializer(isValueTuple, itemSerializers);
                return new AggregationExpression(expression, ast, resultSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonSerializer CreateTupleSerializer(bool isValueTuple, IEnumerable<IBsonSerializer> itemSerializers)
        {
            return isValueTuple ? ValueTupleSerializer.Create(itemSerializers) : TupleSerializer.Create(itemSerializers);
        }
    }
}
