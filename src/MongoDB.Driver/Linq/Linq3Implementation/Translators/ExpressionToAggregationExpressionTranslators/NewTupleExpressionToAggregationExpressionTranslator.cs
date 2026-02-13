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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class NewTupleExpressionToAggregationExpressionTranslator
    {
        public static bool CanTranslate(NewExpression expression)
        {
            var type = expression.Type;
            return type.IsTuple() || type.IsValueTuple();
        }

        public static TranslatedExpression Translate(TranslationContext context, NewExpression expression)
        {
            if (CanTranslate(expression))
            {
                var arguments = expression.Arguments;
                var tupleType = expression.Type;

                var items = new AstExpression[arguments.Count];
                var itemSerializers = new IBsonSerializer[arguments.Count];
                for (var i = 0; i < arguments.Count; i++)
                {
                    var valueExpression = arguments[i];
                    var valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                    items[i] = valueTranslation.Ast;
                    itemSerializers[i] = valueTranslation.Serializer;
                }

                var ast = AstExpression.ComputedArray(items);
                var tupleSerializer = TupleOrValueTupleSerializer.Create(tupleType, itemSerializers);
                return new TranslatedExpression(expression, ast, tupleSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
