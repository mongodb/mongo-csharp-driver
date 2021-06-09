﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class TakeMethodTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Take))
            {
                var source = expression.Arguments[0];
                var count = expression.Arguments[1];

                Expression skip = null;
                if (source is MethodCallExpression skipExpression && skipExpression.Method.Is(EnumerableMethod.Skip))
                {
                    source = skipExpression.Arguments[0];
                    skip = skipExpression.Arguments[1];
                }

                var translatedSource = ExpressionTranslator.Translate(context, source);
                if (translatedSource.Serializer is IBsonArraySerializer arraySerializer && arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                {
                    var translatedCount = ExpressionTranslator.Translate(context, count);

                    AstExpression translation;
                    if (skip == null)
                    {
                        translation = new AstSliceExpression(translatedSource.Translation, translatedCount.Translation);
                    }
                    else
                    {
                        var translatedSkip = ExpressionTranslator.Translate(context, skip);
                        translation = new AstSliceExpression(translatedSource.Translation, translatedSkip.Translation, translatedCount.Translation);
                    }

                    var itemSerializer = itemSerializationInfo.Serializer;
                    var serializer = IEnumerableSerializer.Create(itemSerializer);

                    return new TranslatedExpression(expression, translation, serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
