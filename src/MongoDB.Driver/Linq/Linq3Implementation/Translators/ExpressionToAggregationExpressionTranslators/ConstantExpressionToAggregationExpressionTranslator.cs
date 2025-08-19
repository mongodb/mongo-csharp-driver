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
        public static TranslatedExpression Translate(TranslationContext context, ConstantExpression constantExpression)
        {
            var constantType = constantExpression.Type;
            var constantSerializer = StandardSerializers.TryGetSerializer(constantType, out var serializer) ? serializer : context.SerializationDomain.LookupSerializer(constantType);
            return Translate(constantExpression, constantSerializer);
       }

        public static TranslatedExpression Translate(ConstantExpression constantExpression, IBsonSerializer constantSerializer)
        {
            var constantValue = constantExpression.Value;
            var serializedValue = constantSerializer.ToBsonValue(constantValue);
            var ast = AstExpression.Constant(serializedValue);
            return new TranslatedExpression(constantExpression, ast, constantSerializer);
        }
    }
}
