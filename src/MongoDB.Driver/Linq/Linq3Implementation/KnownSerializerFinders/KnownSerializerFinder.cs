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

namespace MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;

internal static class KnownSerializerFinder
{
    public static KnownSerializerMap FindKnownSerializers(
        Expression expression,
        ExpressionTranslationOptions translationOptions)
    {
        var knownSerializers = new KnownSerializerMap();
        return FindKnownSerializers(expression, translationOptions, knownSerializers);
    }

    public static KnownSerializerMap FindKnownSerializers(
        Expression expression,
        ExpressionTranslationOptions translationOptions,
        Expression initialNode,
        IBsonSerializer knownSerializer)
    {
        var knownSerializers = new KnownSerializerMap();
        knownSerializers.AddSerializer(initialNode, knownSerializer);
        return FindKnownSerializers(expression, translationOptions, knownSerializers);
    }

    public static KnownSerializerMap FindKnownSerializers(
        Expression expression,
        ExpressionTranslationOptions translationOptions,
        (Expression Node, IBsonSerializer KnownSerializer)[] initialNodes)
    {
        var knownSerializers = new KnownSerializerMap();
        foreach (var (initialNode, knownSerializer) in initialNodes)
        {
            knownSerializers.AddSerializer(initialNode, knownSerializer);

        }
        return FindKnownSerializers(expression, translationOptions, knownSerializers);
    }

    public static KnownSerializerMap FindKnownSerializers(
        Expression expression,
        ExpressionTranslationOptions translationOptions,
        KnownSerializerMap knownSerializers)
    {
        var visitor = new KnownSerializerFinderVisitor(translationOptions, knownSerializers);

        int oldSerializerCount;
        int newSerializerCount;
        do
        {
            visitor.StartNextPass();
            oldSerializerCount = knownSerializers.Count;
            visitor.Visit(expression);
            newSerializerCount = knownSerializers.Count;

            // TODO: prevent infinite loop, throw after 100000 passes?
        }
        while (visitor.Pass == 1 || newSerializerCount > oldSerializerCount); // I don't know yet if this can be done in a single pass

        //#if DEBUG
        var expressionWithUnknownSerializer = UnknownSerializerFinder.FindExpressionWithUnknownSerializer(expression, knownSerializers);
        if (expressionWithUnknownSerializer != null)
        {
            throw new ExpressionNotSupportedException(expressionWithUnknownSerializer, because: "we were unable to determine which serializer to use for the result");
        }
        //#endif

        return knownSerializers;
    }
}
