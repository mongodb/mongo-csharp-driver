using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Driver.Linq.Expressions;

namespace MongoDB.Driver.Linq.Processors.EmbeddedPipeline.MethodCallBinders
{
    using System.Linq;
    using System.Reflection;

    internal class AsQuerableBinder : IMethodCallBinder<EmbeddedPipelineBindingContext>
    {
        public static IEnumerable<MethodInfo> GetSupportedMethods()
        {
            yield return MethodHelper.GetMethodDefinition(() => Queryable.AsQueryable(null));
            yield return MethodHelper.GetMethodDefinition(() => Queryable.AsQueryable<object>(null));
        }

        public Expression Bind(
            PipelineExpression pipeline,
            EmbeddedPipelineBindingContext bindingContext,
            MethodCallExpression node,
            IEnumerable<Expression> arguments)
        {
            return pipeline;
        }
    }
}
