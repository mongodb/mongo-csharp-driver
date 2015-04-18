/* Copyright 2010-2014 MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.MethodCallBinders
{
    internal class OfTypeBinder : MethodCallBinderBase
    {
        public override Expression Bind(ProjectionExpression projection, ProjectionBindingContext context, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            // OfType is two operations in one. First we create WhereExpression
            // for the test.  Second, we want to issue a conversion projection 
            // using the selector parameter of a ProjectionExpression such that
            // the conversion will be propogated to all future expressions.

            var newType = node.Method.GetGenericArguments()[0];

            var parameter = Expression.Parameter(projection.Projector.Type);
            var predicate = Expression.Lambda(
                Expression.TypeIs(parameter, newType),
                parameter);

            var source = BindPredicate(projection, context, projection.Source, predicate);

            var serializer = context.SerializerRegistry.GetSerializer(newType);
            var info = new BsonSerializationInfo(null, serializer, newType);

            var projector = new SerializationExpression(
                Expression.Convert(projection.Projector, newType),
                info);

            return new ProjectionExpression(
                source,
                projector);
        }
    }
}
