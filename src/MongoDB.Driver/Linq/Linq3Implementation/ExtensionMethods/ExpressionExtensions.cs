﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods
{
    internal static class ExpressionExtensions
    {
        public static object Evaluate(this Expression expression)
        {
            if (expression is ConstantExpression constantExpression)
            {
                return constantExpression.Value;
            }
            else
            {
                LambdaExpression lambda = Expression.Lambda(expression);
                Delegate fn = lambda.Compile();
                return fn.DynamicInvoke(null);
            }
        }

        public static (string CollectionName, IBsonSerializer DocumentSerializer) GetCollectionInfo(this Expression innerExpression, Expression containerExpression)
        {
            if (innerExpression is ConstantExpression constantExpression &&
                constantExpression.Value is IMongoQueryable mongoQueryable &&
                mongoQueryable.Provider is IMongoQueryProvider mongoQueryProvider &&
                mongoQueryProvider.CollectionNamespace != null)
            {
                return (mongoQueryProvider.CollectionNamespace.CollectionName, mongoQueryProvider.PipelineInputSerializer);
            }

            var message = $"inner expression is not an IMongoQueryable representing a collection";
            throw new ExpressionNotSupportedException(innerExpression, containerExpression, because: message);
        }

        public static TValue GetConstantValue<TValue>(this Expression expression, Expression containingExpression)
        {
            if (expression is ConstantExpression constantExpression)
            {
                return (TValue)constantExpression.Value;
            }

            var message = $"Expression must be a constant: {expression} in {containingExpression}.";
            throw new ExpressionNotSupportedException(message);
        }
    }
}
