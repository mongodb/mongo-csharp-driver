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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class OfTypeMethodToPipelineTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.OfType))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);

                var sourceType = sourceExpression.Type;
                var nominalType = sourceType.GetGenericArguments()[0];
                var actualType = method.GetGenericArguments()[0];

                if (nominalType == actualType)
                {
                    return pipeline;
                }

                var discriminatorConvention = pipeline.OutputSerializer.GetDiscriminatorConvention();
                var discriminatorElementName = discriminatorConvention.ElementName;
                var wrappedValueOutputSerializer = pipeline.OutputSerializer as IWrappedValueSerializer;
                if (wrappedValueOutputSerializer != null)
                {
                    discriminatorConvention = wrappedValueOutputSerializer.ValueSerializer.GetDiscriminatorConvention();
                    discriminatorElementName = wrappedValueOutputSerializer.FieldName + "." + discriminatorElementName;
                }
                var discriminatorField = AstFilter.Field(discriminatorElementName);

                var filter = discriminatorConvention switch
                {
                    IHierarchicalDiscriminatorConvention hierarchicalDiscriminatorConvention => DiscriminatorAstFilter.TypeIs(discriminatorField, hierarchicalDiscriminatorConvention, nominalType, actualType),
                    IScalarDiscriminatorConvention scalarDiscriminatorConvention => DiscriminatorAstFilter.TypeIs(discriminatorField, scalarDiscriminatorConvention, nominalType, actualType),
                    _ => throw new ExpressionNotSupportedException(expression, because: "OfType is not supported with the configured discriminator convention")
                };

                var resultSerializer = context.SerializationDomain.LookupSerializer(actualType);
                if (wrappedValueOutputSerializer != null)
                {
                    resultSerializer = WrappedValueSerializer.Create(wrappedValueOutputSerializer.FieldName, resultSerializer);
                }

                pipeline = pipeline.AddStage(
                    AstStage.Match(filter),
                    resultSerializer);

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
