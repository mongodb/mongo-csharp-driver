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
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Stages;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators
{
    internal static class LookupMethodToPipelineTranslator
    {
        // private static fields
        private static readonly MethodInfo[] __lookupMethods =
        {
            MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignField,
            MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline,
            MongoQueryableMethod.LookupWithDocumentsAndPipeline,
            MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignField,
            MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignFieldAndPipeline,
            MongoQueryableMethod.LookupWithFromAndPipeline
        };

        private static readonly MethodInfo[] __lookupMethodsWithDocuments =
        {
            MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignField,
            MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline,
            MongoQueryableMethod.LookupWithDocumentsAndPipeline
        };

        private static readonly MethodInfo[] __lookupMethodsWithDocumentsAndPipeline =
        {
            MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline,
            MongoQueryableMethod.LookupWithDocumentsAndPipeline
        };

        private static readonly MethodInfo[] __lookupMethodsWithFrom =
        {
            MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignField,
            MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignFieldAndPipeline,
            MongoQueryableMethod.LookupWithFromAndPipeline
        };

        private static readonly MethodInfo[] __lookupMethodsWithFromAndPipeline =
        {
            MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignFieldAndPipeline,
            MongoQueryableMethod.LookupWithFromAndPipeline
        };

        private static readonly MethodInfo[] __lookupMethodsWithLocalFieldAndForeignField =
        {
            MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignField,
            MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline,
            MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignField,
            MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignFieldAndPipeline
        };

        // public static methods
        public static TranslatedPipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__lookupMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);
                ClientSideProjectionHelper.ThrowIfClientSideProjection(expression, pipeline, method);
                var localSerializer = pipeline.OutputSerializer;

                var wrapLocalStage = AstStage.Project(
                    AstProject.Set("_local", AstExpression.RootVar),
                    AstProject.Exclude("_id"));
                var wrappedLocalSerializer = WrappedValueSerializer.Create("_local", localSerializer);

                IMongoCollection foreignCollection = null;
                string foreignCollectionName = null;
                IBsonSerializer foreignSerializer = null;
                if (method.IsOneOf(__lookupMethodsWithFrom))
                {
                    var fromExpression = arguments[1];
                    foreignCollection = fromExpression.GetConstantValue<IMongoCollection>(expression);
                    foreignCollectionName = foreignCollection.CollectionNamespace.CollectionName;
                    foreignSerializer = foreignCollection.DocumentSerializer;
                }

                TranslatedPipeline lookupPipeline = null;
                var isCorrelatedSubquery = false;
                if (method.IsOneOf(__lookupMethodsWithDocuments))
                {
                    var documentsLambda = ExpressionHelper.UnquoteLambda(arguments[1]);
                    var documentsPipeline = TranslateDocuments(context, documentsLambda, localSerializer);
                    var documentSerializer = documentsPipeline.OutputSerializer;

                    if (method.IsOneOf(__lookupMethodsWithDocumentsAndPipeline))
                    {
                        var pipelineLambda = ExpressionHelper.UnquoteLambda(arguments.Last());
                        var localParameter = pipelineLambda.Parameters.First();
                        lookupPipeline = TranslateDocumentsPipeline(context, pipelineLambda, localSerializer, documentSerializer);
                        isCorrelatedSubquery |= pipelineLambda.LambdaBodyReferencesParameter(localParameter);

                        lookupPipeline = new TranslatedPipeline(
                            new AstPipeline(documentsPipeline.Ast.Stages.Concat(lookupPipeline.Ast.Stages)), // splice in the $documents stage
                            lookupPipeline.OutputSerializer);
                    }
                    else
                    {
                        lookupPipeline = documentsPipeline;
                    }

                    foreignSerializer = documentSerializer;
                }

                string localField = null;
                string foreignField = null;
                if (method.IsOneOf(__lookupMethodsWithLocalFieldAndForeignField))
                {
                    var localFieldExpression = ExpressionHelper.UnquoteLambda(arguments[2]);
                    var foreignFieldExpression = ExpressionHelper.UnquoteLambda(arguments[3]);
                    localField = localFieldExpression.TranslateToDottedFieldName(context, wrappedLocalSerializer);
                    foreignField = foreignFieldExpression.TranslateToDottedFieldName(context, foreignSerializer);
                }

                if (method.IsOneOf(__lookupMethodsWithFromAndPipeline))
                {
                    var pipelineLamda = ExpressionHelper.UnquoteLambda(arguments.Last());
                    var localParameter = pipelineLamda.Parameters[0];
                    lookupPipeline = TranslateLookupPipeline(context, pipelineLamda, localSerializer, foreignCollection);
                    isCorrelatedSubquery |= pipelineLamda.LambdaBodyReferencesParameter(localParameter);
                }

                AstComputedField[] let = null;
                if (isCorrelatedSubquery)
                {
                    var localAst = AstExpression.GetField(AstExpression.RootVar, "_local");
                    let = new[] { AstExpression.ComputedField("local", localAst) };
                }

                AstStage lookupStage = null;
                IBsonSerializer resultSerializer = null;
                if (method.Is(MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignField))
                {
                    lookupStage = AstStage.Lookup(
                        foreignCollectionName,
                        localField,
                        foreignField,
                        @as: "_results");

                    resultSerializer = foreignSerializer;
                }
                else if (method.Is(MongoQueryableMethod.LookupWithFromAndLocalFieldAndForeignFieldAndPipeline))
                {
                    lookupStage = AstStage.Lookup(
                        foreignCollectionName,
                        localField,
                        foreignField,
                        let, // will be null if subquery is uncorrelated
                        lookupPipeline.Ast,
                        @as: "_results");

                    resultSerializer = lookupPipeline.OutputSerializer;
                }
                else if (method.Is(MongoQueryableMethod.LookupWithFromAndPipeline))
                {
                    lookupStage = AstStage.Lookup(
                        foreignCollectionName,
                        let, // will be null if subquery is uncorrelated
                        lookupPipeline.Ast,
                        @as: "_results");

                    resultSerializer = lookupPipeline.OutputSerializer;
                }
                else if (method.Is(MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignField))
                {
                    lookupStage = AstStage.Lookup(
                        localField,
                        foreignField,
                        let, // will be null if subquery is uncorrelated
                        lookupPipeline.Ast,
                        @as: "_results");

                    resultSerializer = lookupPipeline.OutputSerializer;
                }
                else if (method.Is(MongoQueryableMethod.LookupWithDocumentsAndLocalFieldAndForeignFieldAndPipeline))
                {
                    lookupStage = AstStage.Lookup(
                        localField,
                        foreignField,
                        let, // will be null if subquery is uncorrelated
                        lookupPipeline.Ast,
                        @as: "_results");

                    resultSerializer = lookupPipeline.OutputSerializer;
                }
                else if (method.Is(MongoQueryableMethod.LookupWithDocumentsAndPipeline))
                {
                    lookupStage = AstStage.Lookup(
                        let, // will be null if subquery is uncorrelated
                        lookupPipeline.Ast,
                        @as: "_results");

                    resultSerializer = lookupPipeline.OutputSerializer;
                }

                if (lookupStage != null)
                {
                    var lookupResultSerializer = LookupResultSerializer.Create(localSerializer, resultSerializer);

                    pipeline = pipeline.AddStages(
                        wrapLocalStage,
                        lookupStage,
                        lookupResultSerializer);

                    return pipeline;
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static TranslatedPipeline TranslateDocuments(
            TranslationContext context,
            LambdaExpression documentsLambda,
            IBsonSerializer localSerializer)
        {
            var localParameter = documentsLambda.Parameters.Single();
            var localAst = AstExpression.GetField(AstExpression.RootVar, "_local");
            var localSymbol = context.CreateSymbol(localParameter, localAst, localSerializer);
            context = context.WithSymbol(localSymbol);
            var documentsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, documentsLambda.Body);
            var documentSerializer = ArraySerializerHelper.GetItemSerializer(documentsTranslation.Serializer);
            var documentsStage = AstStage.Documents(documentsTranslation.Ast);
            return new TranslatedPipeline(new AstPipeline([documentsStage]), documentSerializer);
        }

        private static TranslatedPipeline TranslateDocumentsPipeline(
            TranslationContext context,
            LambdaExpression pipelineLambda,
            IBsonSerializer localSerializer,
            IBsonSerializer documentsSerializer)
        {
            var funcType = pipelineLambda.GetType().GetGenericArguments().Single();
            var funcTypeGenericArguments = funcType.GetGenericArguments();
            var localType = funcTypeGenericArguments[0];
            var documentType = funcTypeGenericArguments[1].GetGenericArguments()[0]; // IQueryable<TDocument>
            var resultType = funcTypeGenericArguments[2].GetGenericArguments()[0]; // IQueryable<TResult>
            var methodInfo = typeof(LookupMethodToPipelineTranslator).GetMethod(nameof(TranslateDocumentsPipelineGeneric), BindingFlags.Static | BindingFlags.NonPublic);
            var genericMethodInfo = methodInfo.MakeGenericMethod(localType, documentType, resultType);
            return (TranslatedPipeline)genericMethodInfo.Invoke(null, [context, pipelineLambda, localSerializer, documentsSerializer]);
        }

        private static TranslatedPipeline TranslateDocumentsPipelineGeneric<TLocal, TDocument, TResult>(
            TranslationContext context,
            Expression<Func<TLocal, IQueryable<TDocument>, IQueryable<TResult>>> pipelineLambda,
            IBsonSerializer<TLocal> localSerializer,
            IBsonSerializer<TDocument> documentSerializer)
        {
            var parameters = pipelineLambda.Parameters;
            var localParameter = parameters[0];
            var queryableParameter = parameters[1];
            var body = pipelineLambda.Body;

            context = TranslationContext.Create(context.TranslationOptions, context.SerializationDomain);
            var localAst = AstExpression.Var("local");
            var localSymbol = context.CreateSymbol(localParameter, localAst, localSerializer);
            context = context.WithSymbol(localSymbol);

            var provider = new MongoQueryProvider<TDocument>(documentSerializer, session: null, options: null);
            var queryable = new MongoQuery<TDocument, TDocument>(provider);

            body = ExpressionReplacer.Replace(body, queryableParameter, Expression.Constant(queryable));
            body = PartialEvaluator.EvaluatePartially(body);

            return ExpressionToPipelineTranslator.Translate(context, body);
        }

        private static TranslatedPipeline TranslateLookupPipeline(
            TranslationContext context,
            LambdaExpression pipelineLambda,
            IBsonSerializer localSerializer,
            IMongoCollection foreignCollection)
        {
            var funcType = pipelineLambda.GetType().GetGenericArguments()[0];
            var funcParameterTypes = funcType.GetGenericArguments();
            var localType = funcParameterTypes[0]; // TLocal
            var foreignType = funcParameterTypes[1].GetGenericArguments()[0]; // IQueryable<TForeign>
            var resultType = funcParameterTypes[2].GetGenericArguments()[0]; // IQueryable<TResult>
            var methodInfo = typeof(LookupMethodToPipelineTranslator).GetMethod(nameof(TranslateLookupPipelineAgainstForeignCollection), BindingFlags.Static | BindingFlags.NonPublic);
            var genericMethodInfo = methodInfo.MakeGenericMethod(localType, foreignType, resultType);
            return (TranslatedPipeline)genericMethodInfo.Invoke(null, [context, pipelineLambda, localSerializer, foreignCollection]);
        }

        private static TranslatedPipeline TranslateLookupPipelineAgainstForeignCollection<TLocal, TForeign, TResult>(
            TranslationContext context,
            Expression<Func<TLocal, IQueryable<TForeign>, IQueryable<TResult>>> pipelineLambda,
            IBsonSerializer<TLocal> localSerializer,
            IMongoCollection<TForeign> foreignCollection)
        {
            var queryable = foreignCollection.AsQueryable();
            return TranslateLookupPipelineAgainstQueryable(context, pipelineLambda, localSerializer, queryable);
        }

        private static TranslatedPipeline TranslateLookupPipelineAgainstQueryable<TLocal, TForeign, TResult>(
            TranslationContext context,
            Expression<Func<TLocal, IQueryable<TForeign>, IQueryable<TResult>>> pipelineLambda,
            IBsonSerializer<TLocal> localSerializer,
            IQueryable<TForeign> queryable)
        {
            var parameters = pipelineLambda.Parameters;
            var localParameter = parameters[0];
            var queryableParameter = parameters[1];
            var body = pipelineLambda.Body;

            context = TranslationContext.Create(context.TranslationOptions, context.SerializationDomain);
            var localAst = AstExpression.Var("local");
            var localSymbol = context.CreateSymbol(localParameter, localAst, localSerializer);
            context = context.WithSymbol(localSymbol);

            body = ExpressionReplacer.Replace(body, queryableParameter, Expression.Constant(queryable));
            body = PartialEvaluator.EvaluatePartially(body);

            var pipeline = ExpressionToPipelineTranslator.Translate(context, body);
            return pipeline;
        }
    }
}
