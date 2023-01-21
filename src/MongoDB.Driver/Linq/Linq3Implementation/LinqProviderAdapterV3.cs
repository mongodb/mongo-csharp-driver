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

using System;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation
{
    internal sealed class LinqProviderAdapterV3 : LinqProviderAdapter
    {
        internal override IMongoQueryable<TDocument> AsQueryable<TDocument>(
            IMongoCollection<TDocument> collection,
            IClientSessionHandle session,
            AggregateOptions options)
        {
            var provider = new MongoQueryProvider<TDocument>(collection, session, options);
            return new MongoQuery<TDocument, TDocument>(provider);
        }

        internal override IMongoQueryable<NoPipelineInput> AsQueryable(
            IMongoDatabase database,
            IClientSessionHandle session,
            AggregateOptions options)
        {
            var provider = new MongoQueryProvider<NoPipelineInput>(database, session, options);
            return new MongoQuery<NoPipelineInput, NoPipelineInput>(provider);
        }

        public override string ToString() => "V3";

        internal override BsonValue TranslateExpressionToAggregateExpression<TSource, TResult>(
            Expression<Func<TSource, TResult>> expression,
            IBsonSerializer<TSource> sourceSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions,
            TranslationContextData contextData = null)
        {
            expression = (Expression<Func<TSource, TResult>>)PartialEvaluator.EvaluatePartially(expression);
            var context = TranslationContext.Create(expression, sourceSerializer, contextData);
            var translation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, expression, sourceSerializer, asRoot: true);
            var simplifiedAst = AstSimplifier.Simplify(translation.Ast);

            return simplifiedAst.Render();
        }

        internal override RenderedProjectionDefinition<TOutput> TranslateExpressionToBucketOutputProjection<TInput, TValue, TOutput>(
            Expression<Func<TInput, TValue>> valueExpression,
            Expression<Func<IGrouping<TValue, TInput>, TOutput>> outputExpression,
            IBsonSerializer<TInput> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            // TODO: implement using LINQ3 instead of falling back to LINQ2
            return LinqProviderAdapter.V2.TranslateExpressionToBucketOutputProjection(valueExpression, outputExpression, documentSerializer, serializerRegistry, translationOptions);
        }

        internal override RenderedFieldDefinition TranslateExpressionToField<TDocument>(
            LambdaExpression expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            var parameter = expression.Parameters.Single();
            var context = TranslationContext.Create(expression, documentSerializer);
            var symbol = context.CreateSymbol(parameter, documentSerializer, isCurrent: true);
            context = context.WithSymbol(symbol);
            var body = RemovePossibleConvertToObject(expression.Body);
            var field = ExpressionToFilterFieldTranslator.Translate(context, body);

            return new RenderedFieldDefinition(field.Path, field.Serializer);

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

        internal override RenderedFieldDefinition<TField> TranslateExpressionToField<TDocument, TField>(
            Expression<Func<TDocument, TField>> expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            bool allowScalarValueForArrayField)
        {
            expression = (Expression<Func<TDocument, TField>>)PartialEvaluator.EvaluatePartially(expression);
            var parameter = expression.Parameters.Single();
            var context = TranslationContext.Create(expression, documentSerializer);
            var symbol = context.CreateSymbol(parameter, documentSerializer, isCurrent: true);
            context = context.WithSymbol(symbol);
            var field = ExpressionToFilterFieldTranslator.Translate(context, expression.Body);

            var underlyingSerializer = field.Serializer;
            var fieldSerializer = underlyingSerializer as IBsonSerializer<TField>;
            var valueSerializer = (IBsonSerializer<TField>)FieldValueSerializerHelper.GetSerializerForValueType(underlyingSerializer, serializerRegistry, typeof(TField), allowScalarValueForArrayField);

            return new RenderedFieldDefinition<TField>(field.Path, fieldSerializer, valueSerializer, underlyingSerializer);
        }

        internal override BsonDocument TranslateExpressionToFilter<TDocument>(
            Expression<Func<TDocument, bool>> expression,
            IBsonSerializer<TDocument> documentSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            expression = (Expression<Func<TDocument, bool>>)PartialEvaluator.EvaluatePartially(expression);
            var context = TranslationContext.Create(expression, documentSerializer);
            var filter = ExpressionToFilterTranslator.TranslateLambda(context, expression, documentSerializer, asRoot: true);
            filter = AstSimplifier.SimplifyAndConvert(filter);

            return filter.Render().AsBsonDocument;
        }

        internal override RenderedProjectionDefinition<TProjection> TranslateExpressionToFindProjection<TSource, TProjection>(
            Expression<Func<TSource, TProjection>> expression,
            IBsonSerializer<TSource> sourceSerializer,
            IBsonSerializerRegistry serializerRegistry)
        {
            return TranslateExpressionToProjection(expression, sourceSerializer, serializerRegistry, translationOptions: null);
        }

        internal override RenderedProjectionDefinition<TOutput> TranslateExpressionToGroupProjection<TInput, TKey, TOutput>(
            Expression<Func<TInput, TKey>> idExpression,
            Expression<Func<IGrouping<TKey, TInput>, TOutput>> groupExpression,
            IBsonSerializer<TInput> documentSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            throw new InvalidOperationException("This method is only valid when using LINQ2.");
        }

        internal override RenderedProjectionDefinition<TOutput> TranslateExpressionToProjection<TInput, TOutput>(
            Expression<Func<TInput, TOutput>> expression,
            IBsonSerializer<TInput> inputSerializer,
            IBsonSerializerRegistry serializerRegistry,
            ExpressionTranslationOptions translationOptions)
        {
            expression = (Expression<Func<TInput, TOutput>>)PartialEvaluator.EvaluatePartially(expression);
            var context = TranslationContext.Create(expression, inputSerializer);
            var translation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, expression, inputSerializer, asRoot: true);
            var (projectStage, projectionSerializer) = ProjectionHelper.CreateProjectStage(translation);
            var simplifiedProjectStage = AstSimplifier.Simplify(projectStage);
            var renderedProjection = simplifiedProjectStage.Render().AsBsonDocument["$project"].AsBsonDocument;

            return new RenderedProjectionDefinition<TOutput>(renderedProjection, (IBsonSerializer<TOutput>)projectionSerializer);
        }
    }
}
