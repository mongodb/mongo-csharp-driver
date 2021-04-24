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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq2;
using MongoDB.Driver.Linq2.Expressions;
using MongoDB.Driver.Linq2.Processors;
using MongoDB.Driver.Linq2.Translators;

namespace MongoDB.Driver.Linq
{
    internal sealed class LinqProviderV2 : LinqProvider
    {
        internal override IMongoQueryable<TDocument> AsQueryable<TDocument>(
            IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            AggregateOptions options,
            CancellationToken cancellationToken)
        {
            var queryProvider = new Linq2.MongoQueryProviderImpl<TDocument>(collection, session, options);
            return new Linq2.MongoQueryableImpl<TDocument, TDocument>(queryProvider);
        }

        public override string ToString() => "V2";

        internal override BsonValue TranslateExpressionToAggregateExpression<TSource, TResult>(
            Expression<Func<TSource, TResult>> expression,
            IBsonSerializer<TSource> sourceSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            return AggregateExpressionTranslator.Translate(expression, sourceSerializer, serializerRegistry, translationOptions);
        }

        internal override RenderedProjectionDefinition<TOutput> TranslateExpressionToBucketOutputProjection<TInput, TValue, TOutput>(
            Expression<Func<TInput, TValue>> valueExpression,
            Expression<Func<IGrouping<TValue, TInput>, TOutput>> outputExpression,
            IBsonSerializer<TInput> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            var renderedOutput = AggregateGroupTranslator.Translate<TValue, TInput, TOutput>(valueExpression, outputExpression, documentSerializer, serializerRegistry, translationOptions);
            var document = renderedOutput.Document;
            document.Remove("_id");
            return new RenderedProjectionDefinition<TOutput>(document, renderedOutput.ProjectionSerializer);
        }

        internal override RenderedFieldDefinition TranslateExpressionToField<TDocument>(
            LambdaExpression expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            var bindingContext = new PipelineBindingContext(serializerRegistry);
            var lambda = ExpressionHelper.GetLambda(PartialEvaluator.Evaluate(expression));
            var parameterExpression = new DocumentExpression(documentSerializer);
            bindingContext.AddExpressionMapping(lambda.Parameters[0], parameterExpression);
            var bound = bindingContext.Bind(lambda.Body);
            bound = FieldExpressionFlattener.FlattenFields(bound);
            IFieldExpression field;
            if (!ExpressionHelper.TryGetExpression(bound, out field))
            {
                var message = string.Format("Unable to determine the serialization information for {0}.", expression);
                throw new InvalidOperationException(message);
            }

            return new RenderedFieldDefinition(field.FieldName, field.Serializer);
        }

        internal override RenderedFieldDefinition<TField> TranslateExpressionToField<TDocument, TField>(
            Expression<Func<TDocument, TField>> expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            bool allowScalarValueForArrayField)
        {
            var lambda = (LambdaExpression)PartialEvaluator.Evaluate(expression);
            var bindingContext = new PipelineBindingContext(serializerRegistry);
            var parameterExpression = new DocumentExpression(documentSerializer);
            bindingContext.AddExpressionMapping(lambda.Parameters[0], parameterExpression);
            var bound = bindingContext.Bind(lambda.Body);
            bound = FieldExpressionFlattener.FlattenFields(bound);
            IFieldExpression field;
            if (!Linq2.ExpressionHelper.TryGetExpression(bound, out field))
            {
                var message = string.Format("Unable to determine the serialization information for {0}.", expression);
                throw new InvalidOperationException(message);
            }

            var underlyingSerializer = field.Serializer;
            var fieldSerializer = underlyingSerializer as IBsonSerializer<TField>;
            var valueSerializer = (IBsonSerializer<TField>)FieldValueSerializerHelper.GetSerializerForValueType(underlyingSerializer, serializerRegistry, typeof(TField), allowScalarValueForArrayField);

            return new RenderedFieldDefinition<TField>(field.FieldName, fieldSerializer, valueSerializer, underlyingSerializer);
        }

        internal override BsonDocument TranslateExpressionToFilter<TDocument>(
            Expression<Func<TDocument, bool>> expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            return PredicateTranslator.Translate<TDocument>(expression, documentSerializer, serializerRegistry);
        }

        internal override RenderedProjectionDefinition<TProjection> TranslateExpressionToFindProjection<TSource, TProjection>(
            Expression<Func<TSource, TProjection>> expression,
            IBsonSerializer<TSource> sourceSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            return FindProjectionTranslator.Translate<TSource, TProjection>(expression, sourceSerializer, serializerRegistry);
        }

        internal override RenderedProjectionDefinition<TOutput> TranslateExpressionToGroupProjection<TInput, TKey, TOutput>(
            Expression<Func<TInput, TKey>> idExpression,
            Expression<Func<IGrouping<TKey, TInput>, TOutput>> groupExpression,
            IBsonSerializer<TInput> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            return AggregateGroupTranslator.Translate<TKey, TInput, TOutput>(idExpression, groupExpression, documentSerializer, serializerRegistry, translationOptions);
        }

        internal override RenderedProjectionDefinition<TOutput> TranslateExpressionToProjection<TInput, TOutput>(
            Expression<Func<TInput, TOutput>> expression,
            IBsonSerializer<TInput> inputSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            return AggregateProjectTranslator.Translate<TInput, TOutput>(expression, inputSerializer, serializerRegistry, translationOptions);
        }
    }
}
