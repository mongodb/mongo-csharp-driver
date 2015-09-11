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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.Pipeline.MethodCallBinders
{
    internal sealed class SelectManyBinder : IMethodCallBinder<PipelineBindingContext>
    {
        public static IEnumerable<MethodInfo> GetSupportedMethods()
        {
            yield return MethodHelper.GetMethodDefinition(() => Enumerable.SelectMany(null, (Func<object, IEnumerable<object>>)null));
            yield return MethodHelper.GetMethodDefinition(() => Enumerable.SelectMany(null, (Func<object, IEnumerable<object>>)null, (Func<object, object, object>)null));
            yield return MethodHelper.GetMethodDefinition(() => Queryable.SelectMany(null, (Expression<Func<object, IEnumerable<object>>>)null));
            yield return MethodHelper.GetMethodDefinition(() => Queryable.SelectMany(null, (Expression<Func<object, IEnumerable<object>>>)null, (Expression<Func<object, object, object>>)null));
        }

        public Expression Bind(PipelineExpression pipeline, PipelineBindingContext bindingContext, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            var collectionSelectorLambda = ExpressionHelper.GetLambda(arguments.First());
            bindingContext.AddExpressionMapping(collectionSelectorLambda.Parameters[0], pipeline.Projector);
            var collectionSelector = bindingContext.Bind(collectionSelectorLambda.Body) as IFieldExpression;
            string collectionItemName = collectionSelectorLambda.Parameters[0].Name;

            if (collectionSelector == null)
            {
                var message = string.Format("Unable to determine the serialization information for the collection selector in the tree: {0}", node.ToString());
                throw new NotSupportedException(message);
            }

            var collectionSerializer = collectionSelector.Serializer as IBsonArraySerializer;
            BsonSerializationInfo itemSerializationInfo;
            if (collectionSerializer == null || !collectionSerializer.TryGetItemSerializationInfo(out itemSerializationInfo))
            {
                var message = string.Format("The collection selector's serializer must implement IBsonArraySerializer: {0}", node.ToString());
                throw new NotSupportedException(message);
            }

            string resultItemName;
            Expression resultSelector;
            if (arguments.Count() == 2)
            {
                var resultLambda = ExpressionHelper.GetLambda(arguments.Last());
                bindingContext.AddExpressionMapping(resultLambda.Parameters[0], pipeline.Projector);
                bindingContext.AddExpressionMapping(
                    resultLambda.Parameters[1],
                    new FieldExpression(collectionSelector.FieldName, itemSerializationInfo.Serializer));

                resultItemName = resultLambda.Parameters[1].Name;
                resultSelector = bindingContext.Bind(resultLambda.Body);
            }
            else
            {
                resultItemName = "__p";
                resultSelector = new FieldExpression(collectionSelector.FieldName, itemSerializationInfo.Serializer);
            }

            var projector = bindingContext.BindProjector(ref resultSelector);

            return new PipelineExpression(
                new SelectManyExpression(
                    pipeline.Source,
                    collectionItemName,
                    (Expression)collectionSelector,
                    resultItemName,
                    resultSelector),
                projector);
        }
    }
}
