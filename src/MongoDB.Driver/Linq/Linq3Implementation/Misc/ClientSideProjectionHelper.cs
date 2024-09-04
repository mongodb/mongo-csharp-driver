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
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class ClientSideProjectionHelper
    {
        // public static methods
        public static void ThrowIfClientSideProjection(
            Expression expression,
            AstPipeline pipeline,
            MethodInfo method)
        {
            if (pipeline.OutputSerializer is IClientSideProjectionDeserializer)
            {
                throw new ExpressionNotSupportedException(expression, because: $"{method.Name} cannot follow a client side projection");
            }
        }

        public static void ThrowIfClientSideProjection(
            Expression expression,
            AstPipeline pipeline,
            MethodInfo method,
            string methodOverload)
        {
            if (pipeline.OutputSerializer is IClientSideProjectionDeserializer)
            {
                throw new ExpressionNotSupportedException(expression, because: $"{method.Name} {methodOverload} cannot follow a client side projection");
            }
        }

        public static void ThrowIfClientSideProjection(
            IBsonSerializer inputSerializer,
            string stageName)
        {
            if (inputSerializer is IClientSideProjectionDeserializer)
            {
                throw new NotSupportedException($"A {stageName} stage cannot follow a client side projection");
            }
        }
    }
}
