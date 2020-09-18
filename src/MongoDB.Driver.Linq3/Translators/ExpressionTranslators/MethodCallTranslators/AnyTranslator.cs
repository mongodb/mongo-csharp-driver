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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodCallTranslators
{
    public static class AnyTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var source = expression.Arguments[0];
            var translatedSource = ExpressionTranslator.Translate(context, source);

            if (expression.Method.Is(EnumerableMethod.Any))
            {
                //var translation = new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", translatedSource.Translation), 0 });
                var translation = new AstBinaryExpression(
                    AstBinaryOperator.Gt,
                    new AstUnaryExpression(AstUnaryOperator.Size, translatedSource.Translation),
                    new AstConstantExpression(0));
                return new TranslatedExpression(expression, translation, null);
            }

            if (expression.Method.Is(EnumerableMethod.AnyWithPredicate))
            {
                var predicate = expression.Arguments[1];
                var predicateLambda = (LambdaExpression)predicate;
                var predicateContext = context.WithSymbol(predicateLambda.Parameters[0], new Symbol("$this", translatedSource.Serializer));
                var translatedPredicate = ExpressionTranslator.Translate(predicateContext, predicateLambda.Body);

                //var translation = new BsonDocument("$reduce", new BsonDocument
                //{
                //    { "input", translatedSource.Translation },
                //    { "initialValue", false },
                //    { "in", new BsonDocument("$or", new BsonArray { "$$value", translatedPredicate.Translation }) }
                //});
                var translation = new AstReduceExpression(
                    translatedSource.Translation,
                    new AstConstantExpression(false),
                    new AstNaryExpression(AstNaryOperator.Or, new AstFieldExpression("$$value"), translatedPredicate.Translation));
                return new TranslatedExpression(expression, translation, null);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
