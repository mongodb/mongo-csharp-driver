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
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.Pipeline.MethodCallBinders
{
    internal sealed class SelectBinder : IMethodCallBinder<PipelineBindingContext>
    {
        public static IEnumerable<MethodInfo> GetSupportedMethods()
        {
            yield return MethodHelper.GetMethodDefinition(() => Enumerable.Select(null, (Func<int, int>)null));
            yield return MethodHelper.GetMethodDefinition(() => Queryable.Select(null, (Expression<Func<int, int>>)null));
        }

        public Expression Bind(PipelineExpression pipeline, PipelineBindingContext bindingContext, MethodCallExpression node, IEnumerable<Expression> arguments)
        {
            var lambda = ExpressionHelper.GetLambda(arguments.Single());
            bindingContext.AddExpressionMapping(lambda.Parameters[0], pipeline.Projector);

            var selector = bindingContext.Bind(lambda.Body);

            if (selector == pipeline.Projector)
            {
                return pipeline;
            }

            var serializationExpression = selector as ISerializationExpression;
            Expression projector = selector;
            if (serializationExpression == null && selector.NodeType != ExpressionType.MemberInit && selector.NodeType != ExpressionType.New)
            {
                // the output of a $project stage must be a document, so 
                // if this isn't already a serialization expression and it's not
                // a new expression or member init, then we need to create an 
                // artificial field to project the computation into.
                var wrapped = bindingContext.WrapField(selector, "__fld0");
                selector = wrapped;
                projector = new FieldExpression(wrapped.FieldName, wrapped.Serializer);
            }
            else if (selector.NodeType == ExpressionType.MemberInit || selector.NodeType == ExpressionType.New)
            {
                var serializer = bindingContext.GetSerializer(selector.Type, selector);
                projector = new DocumentExpression(serializer);
            }

            return new PipelineExpression(
                new SelectExpression(
                    pipeline.Source,
                    lambda.Parameters[0].Name,
                    selector),
                projector);
        }
    }
}
