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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class LambdaExpressionExtensions
    {
        public static string TranslateToDottedFieldName(this LambdaExpression fieldSelectorLambda, TranslationContext context, IBsonSerializer parameterSerializer)
        {
            var parameterExpression = fieldSelectorLambda.Parameters.Single();
            if (parameterSerializer.ValueType != parameterExpression.Type)
            {
                throw new ArgumentException($"ValueType '{parameterSerializer.ValueType.FullName}' of parameterSerializer does not match parameter type '{parameterExpression.Type.FullName}'.", nameof(parameterSerializer));
            }
            var parameterSymbol = context.CreateSymbolWithVarName(parameterExpression, varName: "ROOT", parameterSerializer, isCurrent: true);
            var lambdaContext = context.WithSymbol(parameterSymbol);
            var lambdaBody = ConvertHelper.RemoveConvertToObject(fieldSelectorLambda.Body);
            var fieldSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(lambdaContext, lambdaBody);

            if (fieldSelectorTranslation.Ast.CanBeConvertedToFieldPath())
            {
                var path = fieldSelectorTranslation.Ast.ConvertToFieldPath();
                if (path.Length >= 2 && path[0] == '$' && path[1] != '$')
                {
                    return path.Substring(1);
                }
            }

            throw new ExpressionNotSupportedException(fieldSelectorLambda, because: "expression cannot be translated to a dotted field name");
        }
    }
}
