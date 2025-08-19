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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToSetStageTranslators;

namespace MongoDB.Driver.Linq
{
    internal static class LinqProviderAdapter
    {
        internal static IQueryable<TDocument> AsQueryable<TDocument>(
            IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            AggregateOptions options)
        {
            var provider = new MongoQueryProvider<TDocument>(collection, session, options);
            return new MongoQuery<TDocument, TDocument>(provider);
        }

        internal static IQueryable<NoPipelineInput> AsQueryable(
            IMongoDatabase database,
            IClientSessionHandle session,
            AggregateOptions options)
        {
            var provider = new MongoQueryProvider<NoPipelineInput>(database, session, options);
            return new MongoQuery<NoPipelineInput, NoPipelineInput>(provider);
        }

        internal static BsonValue TranslateExpressionToAggregateExpression<TSource, TResult>(
            Expression<Func<TSource, TResult>> expression,
            IBsonSerializer<TSource> sourceSerializer,
            IBsonSerializationDomain serializationDomain,
            ExpressionTranslationOptions translationOptions,
            TranslationContextData contextData = null)
        {
            expression = (Expression<Func<TSource, TResult>>)PartialEvaluator.EvaluatePartially(expression);
            var context = TranslationContext.Create(translationOptions, serializationDomain, contextData);
            var translation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, expression, sourceSerializer, asRoot: true);
            var simplifiedAst = AstSimplifier.Simplify(translation.Ast);

            return simplifiedAst.Render();
        }

        internal static RenderedFieldDefinition TranslateExpressionToField<TDocument>(
            LambdaExpression expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializationDomain serializationDomain,
            ExpressionTranslationOptions translationOptions)
        {
            expression = (LambdaExpression)PartialEvaluator.EvaluatePartially(expression);
            var parameter = expression.Parameters.Single();
            var context = TranslationContext.Create(translationOptions, serializationDomain);
            var symbol = context.CreateSymbol(parameter, documentSerializer, isCurrent: true);
            context = context.WithSymbol(symbol);
            var body = RemovePossibleConvertToObject(expression.Body);
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, body);

            return new RenderedFieldDefinition(fieldTranslation.Ast.Path, fieldTranslation.Serializer);

            static Expression RemovePossibleConvertToObject(Expression expression)
            {
                if (expression is UnaryExpression unaryExpression &&
                    unaryExpression.NodeType == ExpressionType.Convert &&
                    unaryExpression.Type == typeof(object))
                {
                    return unaryExpression.Operand;
                }

                return expression;
            }
        }

        internal static RenderedFieldDefinition<TField> TranslateExpressionToField<TDocument, TField>(
            Expression<Func<TDocument, TField>> expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializationDomain serializationDomain,
            ExpressionTranslationOptions translationOptions,
            bool allowScalarValueForArrayField)
        {
            expression = (Expression<Func<TDocument, TField>>)PartialEvaluator.EvaluatePartially(expression);
            var parameter = expression.Parameters.Single();
            var context = TranslationContext.Create(translationOptions, serializationDomain);
            var symbol = context.CreateSymbol(parameter, documentSerializer, isCurrent: true);
            context = context.WithSymbol(symbol);
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, expression.Body);

            var underlyingSerializer = fieldTranslation.Serializer;
            var fieldSerializer = underlyingSerializer as IBsonSerializer<TField>;
            var valueSerializer = (IBsonSerializer<TField>)FieldValueSerializerHelper.GetSerializerForValueType(underlyingSerializer, serializationDomain.SerializerRegistry, typeof(TField), allowScalarValueForArrayField);

            return new RenderedFieldDefinition<TField>(fieldTranslation.Ast.Path, fieldSerializer, valueSerializer, underlyingSerializer);
        }

        internal static BsonDocument TranslateExpressionToElemMatchFilter<TElement>(
            Expression<Func<TElement, bool>> expression,
            IBsonSerializer<TElement> elementSerializer,
            IBsonSerializationDomain serializationDomain,
            ExpressionTranslationOptions translationOptions)
        {
            expression = (Expression<Func<TElement, bool>>)PartialEvaluator.EvaluatePartially(expression);
            var context = TranslationContext.Create(translationOptions, serializationDomain);
            var parameter = expression.Parameters.Single();
            var symbol = context.CreateSymbol(parameter, "@<elem>", elementSerializer);  // @<elem> represents the implied element
            context = context.WithSingleSymbol(symbol); // @<elem> is the only symbol visible inside an $elemMatch
            var filter = ExpressionToFilterTranslator.Translate(context, expression.Body, exprOk: false);
            filter = AstSimplifier.SimplifyAndConvert(filter);

            return filter.Render().AsBsonDocument;
        }

        internal static BsonDocument TranslateExpressionToFilter<TDocument>(
            Expression<Func<TDocument, bool>> expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializationDomain serializationDomain,
            ExpressionTranslationOptions translationOptions)
        {
            expression = (Expression<Func<TDocument, bool>>)PartialEvaluator.EvaluatePartially(expression);
            var context = TranslationContext.Create(translationOptions, serializationDomain);
            var filter = ExpressionToFilterTranslator.TranslateLambda(context, expression, documentSerializer, asRoot: true);
            filter = AstSimplifier.SimplifyAndConvert(filter);

            return filter.Render().AsBsonDocument;
        }

        internal static RenderedProjectionDefinition<TProjection> TranslateExpressionToFindProjection<TSource, TProjection>(
            Expression<Func<TSource, TProjection>> expression,
            IBsonSerializer<TSource> sourceSerializer,
            IBsonSerializationDomain serializationDomain,
            ExpressionTranslationOptions translationOptions)
            => TranslateExpressionToProjection(expression, sourceSerializer, translationOptions, forFind: true, serializationDomain);

        internal static RenderedProjectionDefinition<TOutput> TranslateExpressionToProjection<TInput, TOutput>(
            Expression<Func<TInput, TOutput>> expression,
            IBsonSerializer<TInput> inputSerializer,
            IBsonSerializationDomain serializationDomain,
            ExpressionTranslationOptions translationOptions)
            => TranslateExpressionToProjection(expression, inputSerializer, translationOptions, forFind: false, serializationDomain);

        private static RenderedProjectionDefinition<TOutput> TranslateExpressionToProjection<TInput, TOutput>(
            Expression<Func<TInput, TOutput>> expression,
            IBsonSerializer<TInput> inputSerializer,
            ExpressionTranslationOptions translationOptions,
            bool forFind,
            IBsonSerializationDomain serializationDomain)
        {
            if (expression.Parameters.Count == 1 && expression.Body == expression.Parameters[0])
            {
                // handle x => x as a special case
                return new RenderedProjectionDefinition<TOutput>(null, (IBsonSerializer<TOutput>)inputSerializer);
            }

            expression = (Expression<Func<TInput, TOutput>>)PartialEvaluator.EvaluatePartially(expression);
            var context = TranslationContext.Create(translationOptions, serializationDomain);

            var simplifier = forFind ? new AstFindProjectionSimplifier() : new AstSimplifier();
            try
            {
                var translation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, expression, inputSerializer, asRoot: true);
                var (specifications, projectionSerializer) = forFind ? ProjectionHelper.CreateFindProjection(translation) : ProjectionHelper.CreateAggregationProjection(translation);
                specifications = simplifier.VisitAndConvert(specifications);
                var renderedProjection = new BsonDocument(specifications.Select(specification => specification.RenderAsElement()));
                return new RenderedProjectionDefinition<TOutput>(renderedProjection, (IBsonSerializer<TOutput>)projectionSerializer);
            }
            catch (ExpressionNotSupportedException) when (translationOptions?.EnableClientSideProjections ?? false)
            {
                AstProjectStage projectStage;
                IBsonSerializer projectionSerializer;

                var wireVersion = context.TranslationOptions.CompatibilityLevel.ToWireVersion();
                if (forFind && !Feature.FindProjectionExpressions.IsSupported(wireVersion))
                {
                    projectStage = null;
                    projectionSerializer = ClientSideProjectionDeserializer.Create(inputSerializer, expression);
                }
                else
                {
                    (projectStage, projectionSerializer) = ClientSideProjectionTranslator.CreateProjectSnippetsStage(context, expression, inputSerializer);
                    projectStage = simplifier.VisitAndConvert(projectStage);
                }

                var renderedProjection = projectStage?.Render()["$project"].AsBsonDocument;
                return new RenderedProjectionDefinition<TOutput>(renderedProjection, (IBsonSerializer<TOutput>)projectionSerializer);
            }
        }

        internal static BsonDocument TranslateExpressionToSetStage<TDocument, TFields>(
            Expression<Func<TDocument, TFields>> expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializationDomain serializationDomain,
            ExpressionTranslationOptions translationOptions)
        {
            var context = TranslationContext.Create(translationOptions, serializationDomain); // do not partially evaluate expression
            var parameter = expression.Parameters.Single();
            var symbol = context.CreateRootSymbol(parameter, documentSerializer);
            context = context.WithSymbol(symbol);
            var setStage = ExpressionToSetStageTranslator.Translate(context, documentSerializer, expression);
            var simplifiedSetStage = AstSimplifier.SimplifyAndConvert(setStage);
            return simplifiedSetStage.Render().AsBsonDocument;
        }
    }
}
