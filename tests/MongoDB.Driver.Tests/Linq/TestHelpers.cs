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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;

namespace MongoDB.Driver.Tests.Linq;

internal static class TestHelpers
{
    public static SerializerMap CreateSerializerMap(LambdaExpression expression, IBsonSerializer rootModelSerializer = null)
    {
        var rootModelParameter = expression.Parameters.Single();
        rootModelSerializer ??= BsonSerializer.LookupSerializer(rootModelParameter.Type);
        var nodeSerializers = new SerializerMap();
        nodeSerializers.AddSerializer(rootModelParameter, rootModelSerializer);
        return nodeSerializers;
    }

    public static TranslationContext CreateTranslationContext(LambdaExpression expression, IBsonSerializer rootModelSerializer = null)
    {
        var rootModelParameter = expression.Parameters.Single();
        rootModelSerializer ??= BsonSerializer.LookupSerializer(rootModelParameter.Type);

        var context = TranslationContext.Create(expression.Body, rootModelParameter, rootModelSerializer, null);
        var symbol = context.CreateRootSymbol(rootModelParameter, rootModelSerializer);
        return context.WithSymbol(symbol);
    }

    public static LambdaExpression MakeLambda<T1, TResult>(Expression<Func<T1, TResult>> expression)
    {
        // We must run LinqExpressionPreprocessor on the expression before passing them into SerializerFinder or Translators,
        // both of them are expecting to work with subset of Expressions functionality based on PartialEvaluator and ClrCompatExpressionRewriter output.
        return (LambdaExpression)LinqExpressionPreprocessor.Preprocess(expression);
    }
}

