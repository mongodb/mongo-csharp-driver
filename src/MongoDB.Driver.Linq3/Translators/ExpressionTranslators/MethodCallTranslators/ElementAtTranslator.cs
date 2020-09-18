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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodCallTranslators
{
    public static class ElementAtTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.ElementAt))
            {
                var source = expression.Arguments[0];
                var index = expression.Arguments[1];
                var translatedSource = ExpressionTranslator.Translate(context, source);
                var translatedIndex = ExpressionTranslator.Translate(context, index);

                IBsonSerializer itemSerializer = null;
                if (translatedSource.Serializer is IBsonArraySerializer arraySerializer)
                {
                    if (arraySerializer.TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo))
                    {
                        itemSerializer = serializationInfo.Serializer;
                    }
                }

                //var translation = new BsonDocument("$arrayElemAt", new BsonArray { translatedSource.Translation, translatedIndex.Translation });
                var translation = new AstBinaryExpression(AstBinaryOperator.ArrayElemAt, translatedSource.Translation, translatedIndex.Translation);
                return new TranslatedExpression(expression, translation, itemSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
