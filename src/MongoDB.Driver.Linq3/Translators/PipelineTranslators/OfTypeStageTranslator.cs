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

using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.PipelineTranslators
{
    public static class OfTypeStageTranslator
    {
        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            var source = arguments[0];
            var pipeline = PipelineTranslator.Translate(context, source);

            if (method.Is(QueryableMethod.OfType))
            {
                var sourceExpression = arguments[0];
                var sourceType = sourceExpression.Type;
                var nominalType = sourceType.GetGenericArguments()[0];
                var actualType = method.GetGenericArguments()[0];
                var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(nominalType);
                var discriminatorElementName = discriminatorConvention.ElementName;
                if (pipeline.OutputSerializer is IWrappedValueSerializer)
                {
                    discriminatorElementName = "_v." + discriminatorElementName;
                }
                var discriminatorValue = discriminatorConvention.GetDiscriminator(nominalType, actualType);
                var filter = new AstComparisonFilter(AstComparisonFilterOperator.Eq, new AstFieldExpression(discriminatorElementName), discriminatorValue);
                var actualSerializer = BsonSerializer.LookupSerializer(actualType); // TODO: use known serializer
                if (pipeline.OutputSerializer is IWrappedValueSerializer)
                {
                    actualSerializer = WrappedValueSerializer.Create(actualSerializer);
                }

                pipeline.AddStages(
                    actualSerializer,
                    new AstMatchStage(filter));

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
