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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
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
        public static AstPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.Is(QueryableMethod.OfType))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                var sourceType = sourceExpression.Type;
                var nominalType = sourceType.GetGenericArguments()[0];
                var actualType = method.GetGenericArguments()[0];
                var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(nominalType);
                var discriminatorElementName = discriminatorConvention.ElementName;
                var wrappedValueOutputSerializer = pipeline.OutputSerializer as IWrappedValueSerializer;
                if (wrappedValueOutputSerializer != null)
                {
                    discriminatorElementName = wrappedValueOutputSerializer.FieldName + "." + discriminatorElementName;
                }
                var discriminatorField = AstFilter.Field(discriminatorElementName, BsonValueSerializer.Instance);
                var discriminatorValue = discriminatorConvention.GetDiscriminator(nominalType, actualType);
                if (discriminatorValue.IsBsonArray)
                {
                    discriminatorValue = discriminatorValue.AsBsonArray.Last();
                }

                var filter = AstFilter.Eq(discriminatorField, discriminatorValue); // note: OfType only works with hierarchical discriminators
                var actualSerializer = context.KnownSerializersRegistry.GetSerializer(expression);
                if (wrappedValueOutputSerializer != null)
                {
                    actualSerializer = WrappedValueSerializer.Create(wrappedValueOutputSerializer.FieldName, actualSerializer);
                }

                pipeline = pipeline.AddStages(
                    actualSerializer,
                    AstStage.Match(filter));

                return pipeline;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
