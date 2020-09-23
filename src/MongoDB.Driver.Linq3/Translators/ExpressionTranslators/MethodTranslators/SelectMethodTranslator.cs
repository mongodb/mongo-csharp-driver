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
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class SelectMethodTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Select))
            {
                var source = expression.Arguments[0];
                var selector = expression.Arguments[1];
                var translatedSource = ExpressionTranslator.Translate(context, source);

                if (translatedSource.Serializer is IBsonArraySerializer arraySerializer && arraySerializer.TryGetItemSerializationInfo(out BsonSerializationInfo itemSerializationInfo))
                {
                    var selectorLambda = (LambdaExpression)selector;
                    var selectorParameter = selectorLambda.Parameters[0];
                    var sourceItemSerializer = itemSerializationInfo.Serializer;
                    var selectorContext = context.WithSymbol(selectorParameter, new Symbol("$" + selectorParameter.Name, sourceItemSerializer));
                    var translatedSelector = ExpressionTranslator.Translate(selectorContext, selectorLambda.Body);

                    var resultSerializer = translatedSelector.Serializer ?? BsonSerializer.LookupSerializer(selectorLambda.ReturnType);
                    var enumerableResultSerializer = IEnumerableSerializer.Create(resultSerializer);

                    var translation = new AstMapExpression(
                        translatedSource.Translation,
                        selectorParameter.Name,
                        translatedSelector.Translation);
                    return new TranslatedExpression(expression, translation, enumerableResultSerializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
