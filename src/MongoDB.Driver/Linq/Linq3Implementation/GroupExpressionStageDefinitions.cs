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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Translators;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToPipelineTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation
{
    internal sealed class GroupExpressionStageDefinitions<TInput, TKey, TOutput>
    {
        private readonly Expression<Func<TInput, TKey>> _idExpression;
        private readonly Expression<Func<IGrouping<TKey, TInput>, TOutput>> _groupExpression;
        private readonly GroupStageDefinition _groupStage;
        private readonly ProjectStageDefinition _projectStage;

        public GroupExpressionStageDefinitions(
            Expression<Func<TInput, TKey>> idExpression,
            Expression<Func<IGrouping<TKey, TInput>, TOutput>> groupExpression)
        {
            _idExpression = Ensure.IsNotNull(idExpression, nameof(idExpression));
            _groupExpression = Ensure.IsNotNull(groupExpression, nameof(groupExpression));

            _groupStage = new GroupStageDefinition(idExpression, groupExpression);
            _projectStage = new ProjectStageDefinition(_groupStage);
        }

        public Expression<Func<TInput, TKey>> IdExpression => _idExpression;
        public Expression<Func<IGrouping<TKey, TInput>, TOutput>> GroupExpression => _groupExpression;
        public PipelineStageDefinition<TInput, IGrouping<TKey, TInput>> GroupStage => _groupStage;
        public PipelineStageDefinition<IGrouping<TKey, TInput>, TOutput> ProjectStage => _projectStage;

        private class GroupStageDefinition : PipelineStageDefinition<TInput, IGrouping<TKey, TInput>>
        {
            private readonly Expression<Func<TInput, TKey>> _idExpression;
            private readonly Expression<Func<IGrouping<TKey, TInput>, TOutput>> _groupExpression;
            private RenderedPipelineStageDefinition<TOutput> _renderedProjectStage = null;

            public GroupStageDefinition(
                Expression<Func<TInput, TKey>> idExpression,
                Expression<Func<IGrouping<TKey, TInput>, TOutput>> groupExpression)
            {
                _idExpression = idExpression;
                _groupExpression = groupExpression;
            }

            public override string OperatorName => "$group";
            public RenderedPipelineStageDefinition<TOutput> RenderedProjectStage => _renderedProjectStage;

            public override RenderedPipelineStageDefinition<IGrouping<TKey, TInput>> Render(
                IBsonSerializer<TInput> inputSerializer,
                IBsonSerializerRegistry serializerRegistry,
                LinqProvider linqProvider)
            {
                if (linqProvider != LinqProvider.V3)
                {
                    throw new InvalidOperationException("GroupExpressionStageDefinitions can only be used with LINQ3.");
                }

                var expression = CreateExpression(inputSerializer);
                expression = PartialEvaluator.EvaluatePartially(expression);
                var context = TranslationContext.Create(expression, inputSerializer);
                var unoptimizedPipeline = ExpressionToPipelineTranslator.Translate(context, expression);
                var pipeline = AstPipelineOptimizer.Optimize(unoptimizedPipeline);

                var groupStageDocument = pipeline.Stages[0].Render().AsBsonDocument;
                var renderedGroupStage = new RenderedPipelineStageDefinition<IGrouping<TKey, TInput>>("$group", groupStageDocument, new DummyIGroupingSerializer());

                var projectStageDocument = pipeline.Stages[1].Render().AsBsonDocument;
                _renderedProjectStage = new RenderedPipelineStageDefinition<TOutput>("$project", projectStageDocument, (IBsonSerializer<TOutput>)pipeline.OutputSerializer);

                return renderedGroupStage;
            }

            private Expression CreateExpression(IBsonSerializer inputSerializer)
            {
                var provider = new PseudoQueryProvider(inputSerializer);
                var pseudoSource = new PseudoSource(provider);

                var groupByExpression = Expression.Call(
                    QueryableMethod.GroupByWithKeySelector.MakeGenericMethod(typeof(TInput), typeof(TKey)),
                    Expression.Constant(pseudoSource),
                    _idExpression);

                var selectExpression = Expression.Call(
                    QueryableMethod.Select.MakeGenericMethod(typeof(IGrouping<TKey, TInput>), typeof(TOutput)),
                    groupByExpression,
                    _groupExpression);

                return selectExpression;
            }

            private class PseudoQueryProvider : IMongoQueryProvider
            {
                private readonly IBsonSerializer _inputSerializer;

                public PseudoQueryProvider(IBsonSerializer inputSerializer)
                {
                    _inputSerializer = inputSerializer;
                }

                public CollectionNamespace CollectionNamespace => throw new NotImplementedException();

                public IBsonSerializer CollectionDocumentSerializer => _inputSerializer;

                public IQueryable CreateQuery(Expression expression) => throw new NotImplementedException();
                public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => throw new NotImplementedException();
                public object Execute(Expression expression) => throw new NotImplementedException();
                public TResult Execute<TResult>(Expression expression) => throw new NotImplementedException();
                public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default) => throw new NotImplementedException();
                public QueryableExecutionModel GetExecutionModel(Expression expression) => throw new NotImplementedException();
            }

            private class PseudoSource : IQueryable<TInput>
            {
                private readonly Expression _expression;
                private readonly IQueryProvider _provider;

                public PseudoSource(IQueryProvider provider)
                {
                    _provider = provider;
                    _expression = Expression.Constant(this);
                }

                public Type ElementType => typeof(TInput);

                public Expression Expression => _expression;
                public IQueryProvider Provider => _provider;

                public IEnumerator<TInput> GetEnumerator() => throw new NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
            }

            private class DummyIGroupingSerializer : IBsonSerializer<IGrouping<TKey, TInput>>
            {
                public Type ValueType => typeof(IGrouping<TKey, TInput>);

                public IGrouping<TKey, TInput> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => throw new NotImplementedException();
                public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IGrouping<TKey, TInput> value) => throw new NotImplementedException();
                public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => throw new NotImplementedException();
                object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => throw new NotImplementedException();
            }
        }

        private class ProjectStageDefinition : PipelineStageDefinition<IGrouping<TKey, TInput>, TOutput>
        {
            private readonly GroupStageDefinition _groupStageDefinition;

            public ProjectStageDefinition(GroupStageDefinition groupStageDefinition)
            {
                _groupStageDefinition = groupStageDefinition;
            }

            public override string OperatorName => "$project";

            public override RenderedPipelineStageDefinition<TOutput> Render(IBsonSerializer<IGrouping<TKey, TInput>> inputSerializer, IBsonSerializerRegistry serializerRegistry, LinqProvider linqProvider)
            {
                var renderedProjectStage = _groupStageDefinition.RenderedProjectStage;
                if (renderedProjectStage == null)
                {
                    throw new InvalidOperationException("GroupStageDefinition.Render must be called before ProjectStageDefinition.Render.");
                }
                return renderedProjectStage;
            }
        }
    }
}
