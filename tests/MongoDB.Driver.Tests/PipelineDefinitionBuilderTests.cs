﻿/* Copyright 2017-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Search;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class PipelineDefinitionBuilderTests
    {
        // public methods
        [Theory]
        [InlineData(ChangeStreamFullDocumentOption.Default, null, "{ $changeStream : { } }")]
        [InlineData(ChangeStreamFullDocumentOption.UpdateLookup, null, "{ $changeStream : { fullDocument : \"updateLookup\" } }")]
        [InlineData(ChangeStreamFullDocumentOption.WhenAvailable, null, "{ $changeStream : { fullDocument : \"whenAvailable\" } }")]
        [InlineData(ChangeStreamFullDocumentOption.Required, null, "{ $changeStream : { fullDocument : \"required\" } }")]
        [InlineData(ChangeStreamFullDocumentOption.Default, "{ a : 1 }", "{ $changeStream : { resumeAfter : { a : 1 } } }")]
        [InlineData(ChangeStreamFullDocumentOption.UpdateLookup, "{ a : 1 }", "{ $changeStream : { fullDocument : \"updateLookup\", resumeAfter : { a : 1 } } }")]
        [InlineData(ChangeStreamFullDocumentOption.WhenAvailable, "{ a : 1 }", "{ $changeStream : { fullDocument : \"whenAvailable\", resumeAfter : { a : 1 } } }")]
        [InlineData(ChangeStreamFullDocumentOption.Required, "{ a : 1 }", "{ $changeStream : { fullDocument : \"required\", resumeAfter : { a : 1 } } }")]
        public void ChangeStream_should_add_the_expected_stage(
            ChangeStreamFullDocumentOption fullDocument,
            string resumeAfterString,
            string expectedStage)
        {
            var resumeAfter = resumeAfterString == null ? null : BsonDocument.Parse(resumeAfterString);
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var options = new ChangeStreamStageOptions
            {
                FullDocument = fullDocument,
                ResumeAfter = resumeAfter
            };

            var result = pipeline.ChangeStream(options);

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages.Count.Should().Be(1);
            stages[0].Should().Be(expectedStage);
        }

        [Fact]
        public void ChangeStream_should_add_the_expected_stage_when_options_is_null()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            ChangeStreamStageOptions options = null;

            var result = pipeline.ChangeStream(options);

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages.Count.Should().Be(1);
            stages[0].Should().Be("{ $changeStream : { } }");
        }

        [Fact]
        public void ChangeStream_should_throw_when_pipeline_is_null()
        {
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = null;
            ChangeStreamStageOptions options = null;

            var exception = Record.Exception(() => pipeline.ChangeStream(options));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Lookup_should_throw_when_pipeline_is_null()
        {
            RequireServer.Check();

            PipelineDefinition<BsonDocument, IEnumerable<BsonDocument>> pipeline = null;
            IMongoCollection<BsonDocument> collection = null;

            var exception = Record.Exception(() => pipeline.Lookup(
                collection,
                new BsonDocument(),
                new EmptyPipelineDefinition<BsonDocument>(),
                new StringFieldDefinition<BsonDocument, IEnumerable<BsonDocument>>("asValue")
            ));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Merge_should_add_expected_stage()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var client = DriverTestConfiguration.Client;
            var outputDatabase = client.GetDatabase("database");
            var outputCollection = outputDatabase.GetCollection<BsonDocument>("collection");
            var mergeOptions = new MergeStageOptions<BsonDocument>();

            var result = pipeline.Merge(outputCollection, mergeOptions);

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages.Count.Should().Be(1);
            stages[0].Should().Be("{ $merge : { into : { db : 'database', coll : 'collection' } } }");
        }

        [Fact]
        public void Search_should_add_expected_stage()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var result = pipeline.Search(builder.Text("bar", "foo"));

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $search: { text: { query: 'foo', path: 'bar' } } }");
        }

        [Fact]
        public void Search_should_add_expected_stage_with_highlight()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var result = pipeline.Search(builder.Text("bar", "foo"), new SearchHighlightOptions<BsonDocument>("foo"));

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().BeEquivalentTo("{ $search: { text: { query: 'foo', path: 'bar' }, highlight: { path: 'foo' } } }");
        }

        [Fact]
        public void Search_should_add_expected_stage_with_index()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var result = pipeline.Search(builder.Text("bar", "foo"), indexName: "foo");

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $search: { text: { query: 'foo', path: 'bar' }, index: 'foo' } }");
        }

        [Fact]
        public void Search_should_add_expected_stage_with_count()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();
            var count = new SearchCountOptions()
            {
                Type = SearchCountType.Total
            };

            var result = pipeline.Search(builder.Text("bar", "foo"), count: count);

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $search: { text: { query: 'foo', path: 'bar' }, count: { type: 'total' } } }");
        }

        [Fact]
        public void Search_should_add_expected_stage_with_return_stored_source()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var result = pipeline.Search(builder.Text("bar", "foo"), returnStoredSource: true);

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $search: { text: { query: 'foo', path: 'bar' }, returnStoredSource: true } }");
        }

        [Fact]
        public void Search_should_add_expected_stage_with_score_details()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var result = pipeline.Search(builder.Text("bar", "foo"), scoreDetails: true);

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $search: { text: { query: 'foo', path: 'bar' }, scoreDetails: true } }");
        }

        [Fact]
        public void Search_should_add_expected_stage_with_sort()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();
            var sortBuilder = new SortDefinitionBuilder<BsonDocument>();

            var result = pipeline.Search(builder.Text("bar", "foo"), sort: sortBuilder.Ascending("foo"));

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $search: { text: { query: 'foo', path: 'bar' }, sort: { 'foo': 1 } } }");
        }

        [Fact]
        public void Search_should_throw_when_pipeline_is_null()
        {
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = null;
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var exception = Record.Exception(() => pipeline.Search(builder.Text("bar", "foo")));

            exception.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void Search_should_throw_when_query_is_null()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();

            var exception = Record.Exception(() => pipeline.Search(null));

            exception.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("searchDefinition");
        }

        [Fact]
        public void SearchMeta_should_add_expected_stage()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var result = pipeline.SearchMeta(builder.Text("bar", "foo"));

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $searchMeta: { text: { query: 'foo', path: 'bar' } } }");
        }

        [Fact]
        public void SearchMeta_should_add_expected_stage_with_index()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var result = pipeline.SearchMeta(builder.Text("bar", "foo"), indexName: "foo");

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $searchMeta: { text: { query: 'foo', path: 'bar' }, index: 'foo' } }");
        }

        [Fact]
        public void SearchMeta_should_add_expected_stage_with_count()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var builder = new SearchDefinitionBuilder<BsonDocument>();
            var count = new SearchCountOptions()
            {
                Type = SearchCountType.Total
            };

            var result = pipeline.SearchMeta(builder.Text("bar", "foo"), count: count);

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $searchMeta: { text: { query: 'foo', path: 'bar' }, count: { type: 'total' } } }");
        }

        [Fact]
        public void SearchMeta_should_throw_when_pipeline_is_null()
        {
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = null;
            var builder = new SearchDefinitionBuilder<BsonDocument>();

            var exception = Record.Exception(() => pipeline.SearchMeta(builder.Text("bar", "foo")));

            exception.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void SearchMeta_should_throw_when_query_is_null()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();

            var exception = Record.Exception(() => pipeline.SearchMeta(null));

            exception.Should().BeOfType<ArgumentNullException>()
                .Which.ParamName.Should().Be("searchDefinition");
        }

        [Fact]
        public void UnionWith_should_add_expected_stage()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var withCollection = Mock.Of<IMongoCollection<BsonDocument>>(
                coll => coll.CollectionNamespace == CollectionNamespace.FromFullName("db.test"));
            var withPipeline = new EmptyPipelineDefinition<BsonDocument>()
                .AppendStage<BsonDocument, BsonDocument, BsonDocument>("{ $match : { b : 1 } }");

            var result = pipeline.UnionWith(withCollection, withPipeline);

            var stages = RenderStages(result, BsonDocumentSerializer.Instance);
            stages[0].Should().Be("{ $unionWith : { coll : 'test', pipeline : [{ $match : { b : 1 } }] } }");
        }

        [Fact]
        public void UnionWith_should_throw_when_pipeline_is_null()
        {
            PipelineDefinition<BsonDocument, BsonDocument> pipeline = null;
            IMongoCollection<BsonDocument> withCollection = null;
            var withPipeline = new EmptyPipelineDefinition<BsonDocument>();

            var exception = Record.Exception(() => pipeline.UnionWith(withCollection, withPipeline));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("pipeline");
        }

        [Fact]
        public void UnionWith_should_throw_when_TWith_is_not_the_same_with_TInput_and_withPipeline_is_null()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            var withCollection = Mock.Of<IMongoCollection<object>>(
                coll => coll.CollectionNamespace == CollectionNamespace.FromFullName("db.test"));

            var exception = Record.Exception(() => pipeline.UnionWith(withCollection, withPipeline: null));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().StartWith("The withPipeline cannot be null when TWith != TInput. A pipeline is required to transform the TWith documents to TInput documents.");
            e.ParamName.Should().Be("withPipeline");
        }

        [Fact]
        public void UnionWith_should_throw_when_withCollection_is_null()
        {
            var pipeline = new EmptyPipelineDefinition<BsonDocument>();
            IMongoCollection<BsonDocument> withCollection = null;
            var withPipeline = new EmptyPipelineDefinition<BsonDocument>();

            var exception = Record.Exception(() => pipeline.UnionWith(withCollection, withPipeline));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("withCollection");
        }

        // private methods
        private IList<BsonDocument> RenderStages<TInput, TOutput>(PipelineDefinition<TInput, TOutput> pipeline, IBsonSerializer<TInput> inputSerializer)
        {
            var renderedPipeline = pipeline.Render(inputSerializer, BsonSerializer.SerializerRegistry);
            return renderedPipeline.Documents;
        }
    }
}
