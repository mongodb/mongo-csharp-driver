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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ConstantExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, ConstantExpression expression)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                var constantType = expression.Type;
                var constantSerializer = StandardSerializers.TryGetSerializer(constantType, out var serializer) ? serializer : BsonSerializer.LookupSerializer(constantType);
                return Translate(expression, constantSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static AggregationExpression Translate(ConstantExpression expression, IBsonSerializer constantSerializer)
        {
            if (expression.NodeType == ExpressionType.Constant)
            {
                var constantValue = expression.Value;
                var serializedValue = constantSerializer.ToBsonValue(constantValue);
                var ast = AstExpression.Constant(serializedValue);
                return new AggregationExpression(expression, ast, constantSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
