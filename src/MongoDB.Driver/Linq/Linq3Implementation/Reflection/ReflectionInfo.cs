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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Reflection
{
    internal static class ReflectionInfo
    {
        public static ConstructorInfo Constructor<TObject>(Expression<Func<TObject>> lambda)
        {
            return ExtractConstructorInfoFromLambda(lambda);
        }

        public static ConstructorInfo Constructor<T1, TObject>(Expression<Func<T1, TObject>> lambda)
        {
            return ExtractConstructorInfoFromLambda(lambda);
        }

        public static ConstructorInfo Constructor<T1, T2, TObject>(Expression<Func<T1, T2, TObject>> lambda)
        {
            return ExtractConstructorInfoFromLambda(lambda);
        }

        public static ConstructorInfo Constructor<T1, T2, T3, TObject>(Expression<Func<T1, T2, T3, TObject>> lambda)
        {
            return ExtractConstructorInfoFromLambda(lambda);
        }

        public static ConstructorInfo Constructor<T1, T2, T3, T4, TObject>(Expression<Func<T1, T2, T3, T4, TObject>> lambda)
        {
            return ExtractConstructorInfoFromLambda(lambda);
        }

        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5, TObject>(Expression<Func<T1, T2, T3, T4, T5, TObject>> lambda)
        {
            return ExtractConstructorInfoFromLambda(lambda);
        }

        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5, T6, TObject>(Expression<Func<T1, T2, T3, T4, T5, T6, TObject>> lambda)
        {
            return ExtractConstructorInfoFromLambda(lambda);
        }

        public static ConstructorInfo Constructor<T1, T2, T3, T4, T5, T6, T7, TObject>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TObject>> lambda)
        {
            return ExtractConstructorInfoFromLambda(lambda);
        }

        public static MethodInfo Method<T1, TResult>(Expression<Func<T1, TResult>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static MethodInfo Method<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static MethodInfo Method<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static MethodInfo Method<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static MethodInfo Method<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static MethodInfo Method<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static PropertyInfo Property<TObject, TProperty>(Expression<Func<TObject, TProperty>> lambda)
        {
            return ExtractPropertyInfoFromLambda(lambda);
        }

        public static MethodInfo IndexerGet<TObject, TIndex, TValue>(Expression<Func<TObject, TIndex, TValue>> lambda)
        {
            return ExtractMethodInfoFromLambda(lambda);
        }

        public static MethodInfo IndexerSet<TObject, TIndex, TValue>(Expression<Func<TObject, TIndex, TValue>> lambda)
        {
            return ExtractIndexerSetMethodInfoFromLambda(lambda);
        }

        // private static methods
        private static ConstructorInfo ExtractConstructorInfoFromLambda(LambdaExpression lambda)
        {
            var newExpression = (NewExpression)lambda.Body;
            return newExpression.Constructor;
        }

        private static MethodInfo ExtractIndexerSetMethodInfoFromLambda(LambdaExpression lambda)
        {
            var getMethod = ExtractMethodInfoFromLambda(lambda);
            var declaringType = getMethod.DeclaringType;
            foreach (var propertyInfo in declaringType.GetProperties())
            {
                if (propertyInfo.GetGetMethod() == getMethod)
                {
                    return propertyInfo.GetSetMethod();
                }
            }

            throw new ArgumentException($"No set method found for: {lambda.Body}.", nameof(lambda));
        }

        private static MethodInfo ExtractMethodInfoFromLambda(LambdaExpression lambda)
        {
            var methodCallExpression = (MethodCallExpression)lambda.Body;
            var method = methodCallExpression.Method;

            if (method.IsGenericMethod)
            {
                method = method.GetGenericMethodDefinition();
            }

            return method;
        }

        public static PropertyInfo ExtractPropertyInfoFromLambda(LambdaExpression lambda)
        {
            var propertyExpression = (MemberExpression)lambda.Body;
            var propertyInfo = (PropertyInfo)propertyExpression.Member;

            var declaringType = propertyInfo.DeclaringType;
            if (declaringType.IsConstructedGenericType)
            {
                var declaringTypeDefinition = declaringType.GetGenericTypeDefinition();
                propertyInfo = declaringTypeDefinition.GetProperty(propertyInfo.Name);
            }

            return propertyInfo;
        }
    }
}
