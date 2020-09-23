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
    public static class WhereTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Where))
            {
                var source = expression.Arguments[0];
                var predicate = expression.Arguments[1];
                var translatedSource = ExpressionTranslator.Translate(context, source);

                if (translatedSource.Serializer is IBsonArraySerializer arraySerializer && arraySerializer.TryGetItemSerializationInfo(out BsonSerializationInfo sourceItemSerializationInfo))
                {
                    var predicateLambda = (LambdaExpression)predicate;
                    var predicateParameter = predicateLambda.Parameters[0];
                    var sourceItemSerializer = sourceItemSerializationInfo.Serializer;
                    var predicateContext = context.WithSymbol(predicateParameter, new Symbol("$" + predicateParameter.Name, sourceItemSerializer));
                    var translatedPredicate = ExpressionTranslator.Translate(predicateContext, predicateLambda.Body);

                    var translation = new AstFilterExpression(
                        translatedSource.Translation,
                        translatedPredicate.Translation,
                        predicateParameter.Name);
                    return new TranslatedExpression(expression, translation, translatedSource.Serializer);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
