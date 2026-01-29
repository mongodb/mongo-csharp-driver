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

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal static class SerializerFinder
{
    public static void FindSerializers(
        Expression expression,
        ExpressionTranslationOptions translationOptions,
        SerializerMap nodeSerializers)
    {
        var visitor = new SerializerFinderVisitor(translationOptions, nodeSerializers);

        do
        {
            visitor.StartPass();
            visitor.Visit(expression);
            visitor.EndPass();
        }
        while (visitor.IsMakingProgress);

        //#if DEBUG
        var expressionWithMissingSerializer = MissingSerializerFinder.FindExpressionWithMissingSerializer(expression, nodeSerializers);
        if (expressionWithMissingSerializer != null)
        {
            throw new ExpressionNotSupportedException(expressionWithMissingSerializer, because: "we were unable to determine which serializer to use for the result");
        }
        //#endif
    }
}
