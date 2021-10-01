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
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class LambdaExpressionExtensions
    {
        public static string GetFieldPath(this LambdaExpression fieldSelectorLambda, TranslationContext context, IBsonSerializer parameterSerializer)
        {
            var fieldSelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, fieldSelectorLambda, parameterSerializer, asRoot: true);
            if (fieldSelectorTranslation.Ast.CanBeConvertedToFieldPath())
            {
                var path = fieldSelectorTranslation.Ast.ConvertToFieldPath();
                if (path.Length >= 2 && path[0] == '$' && path[1] != '$')
                {
                    return path.Substring(1);
                }
            }

            throw new ExpressionNotSupportedException(fieldSelectorLambda);
        }
    }
}
