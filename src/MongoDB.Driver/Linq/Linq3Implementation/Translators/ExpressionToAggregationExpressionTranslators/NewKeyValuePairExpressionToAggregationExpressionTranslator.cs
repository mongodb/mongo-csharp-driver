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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewKeyValuePairExpressionToAggregationExpressionTranslator
    {
        public static bool CanTranslate(NewExpression expression)
            => expression.Type.IsConstructedGenericType && expression.Type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

        public static TranslatedExpression Translate(TranslationContext context, NewExpression expression)
        {
            var arguments = expression.Arguments;
            var keyExpression = arguments[0];
            var valueExpression = arguments[1];
            return Translate(context, expression, keyExpression, valueExpression);
        }

        public static TranslatedExpression Translate(
            TranslationContext context,
            Expression expression,
            Expression keyExpression,
            Expression valueExpression)
        {
            var keyTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, keyExpression);
            var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);

            var ast = AstExpression.ComputedDocument([
                AstExpression.ComputedField("Key", keyTranslation.Ast),
                AstExpression.ComputedField("Value", valueTranslation.Ast)
            ]);

            var serializer = CreateResultSerializer(expression.Type, keyTranslation.Serializer, valueTranslation.Serializer);

            return new TranslatedExpression(expression, ast, serializer);
        }

        private static IBsonSerializer CreateResultSerializer(Type resultType, IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
        {
            var constructorInfo = resultType.GetConstructor([keySerializer.ValueType, valueSerializer.ValueType]);
            var classMap = new BsonClassMap(resultType);
            classMap.MapConstructor(constructorInfo);
            classMap.AutoMap();
            classMap.GetMemberMap("Key").SetSerializer(keySerializer);
            classMap.GetMemberMap("Value").SetSerializer(valueSerializer);
            classMap.Freeze();

            // have to use BsonClassMapSerializer here to mimic the MemberInitExpressionToAggregationExpressionTranslator to avoid risking a behavioral breaking change
            var serializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(resultType);
            return (IBsonSerializer)Activator.CreateInstance(serializerType, classMap);
        }
    }
}
