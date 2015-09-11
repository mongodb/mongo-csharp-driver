/* Copyright 2015 MongoDB Inc.
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
* 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq
{
    internal static class MethodHelper
    {
        public static MethodInfo GetMethodInfo<TResult>(Expression<Func<TResult>> lambda)
        {
            Ensure.IsNotNull(lambda, nameof(lambda));

            switch (lambda.Body.NodeType)
            {
                case ExpressionType.Call:
                    return ((MethodCallExpression)lambda.Body).Method;
            }

            throw new MongoInternalException(string.Format("Unable to extract method info from {0}", lambda.Body.ToString()));
        }

        public static MethodInfo GetMethodInfo<T, TResult>(Expression<Func<T, TResult>> lambda)
        {
            Ensure.IsNotNull(lambda, nameof(lambda));

            switch (lambda.Body.NodeType)
            {
                case ExpressionType.Call:
                    return ((MethodCallExpression)lambda.Body).Method;
            }

            throw new MongoInternalException(string.Format("Unable to extract method info from {0}", lambda.Body.ToString()));
        }

        public static MethodInfo GetMethodDefinition<TResult>(Expression<Func<TResult>> lambda)
        {
            var methodInfo = GetMethodInfo(lambda);
            return GetMethodDefinition(methodInfo);
        }

        public static MethodInfo GetMethodDefinition<T, TResult>(Expression<Func<T, TResult>> lambda)
        {
            var methodInfo = GetMethodInfo(lambda);
            return GetMethodDefinition(methodInfo);
        }

        public static IEnumerable<MethodInfo> GetEnumerableAndQueryableMethodDefinitions(string name)
        {
            return typeof(Enumerable)
                .GetMethods()
                .Concat(typeof(Queryable).GetMethods())
                .Concat(typeof(MongoEnumerable).GetMethods())
                .Concat(typeof(MongoQueryable).GetMethods())
                .Where(x => x.Name == name)
                .Select(x => GetMethodDefinition(x));
        }

        public static MethodInfo GetMethodDefinition(MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod && !methodInfo.IsGenericMethodDefinition)
            {
                methodInfo = methodInfo.GetGenericMethodDefinition();
            }

            methodInfo = methodInfo.GetBaseDefinition();

            if (!methodInfo.DeclaringType.IsGenericType)
            {
                return methodInfo;
            }

            var declaringTypeDefinition = methodInfo.DeclaringType.GetGenericTypeDefinition();
            return (MethodInfo)MethodBase.GetMethodFromHandle(methodInfo.MethodHandle, declaringTypeDefinition.TypeHandle);
        }
    }
}
