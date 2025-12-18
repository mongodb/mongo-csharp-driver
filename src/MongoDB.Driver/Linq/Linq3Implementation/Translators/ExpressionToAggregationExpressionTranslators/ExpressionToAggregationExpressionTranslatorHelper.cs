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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

internal static class ExpressionToAggregationExpressionTranslatorHelper
{
    public static AstExpression CreateAggregationAstWithUnwrapping(
        TranslatedExpression sourceTranslation,
        IBsonSerializer sourceItemSerializer,
        Func<AstExpression, AstExpression> createAggregation,
        out IBsonSerializer unwrappedSerializer)
    {
        if (sourceItemSerializer is IWrappedValueSerializer wrappedValueSerializer)
        {
            var itemVar = AstExpression.Var("item");
            var unwrappedItemAst = AstExpression.GetField(itemVar, wrappedValueSerializer.FieldName);
            unwrappedSerializer = wrappedValueSerializer.ValueSerializer;
            return createAggregation(
                AstExpression.Map(
                    input: sourceTranslation.Ast,
                    @as: itemVar,
                    @in: unwrappedItemAst));
        }

        unwrappedSerializer = null;
        return createAggregation(sourceTranslation.Ast);
    }
}