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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Linq3.ExtensionMethods
{
    internal static class ExpressionExtensions
    {
        public static (string CollectionName, IBsonSerializer DocumentSerializer) GetCollectionInfo(this Expression innerExpression, Expression containerExpression)
        {
            if (innerExpression is ConstantExpression constantExpression &&
                constantExpression.Value is IQueryable queryable &&
                queryable.Provider is MongoQueryProvider queryProvider)
            {
                return (queryProvider.CollectionNamespace.CollectionName, queryProvider.CollectionDocumentSerializer);
            }

            var message = $"Expression inner must be a MongoDB queryable representing a collection: {innerExpression} in {containerExpression}.";
            throw new ExpressionNotSupportedException(message);
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
