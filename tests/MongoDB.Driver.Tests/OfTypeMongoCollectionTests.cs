﻿/* Copyright 2013-present MongoDB Inc.
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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class OfTypeMongoCollectionTests
    {
        private BsonDocument _ofTypeFilter;
        private BsonDocument _expectedFilter;
        private BsonDocument _providedFilter;
        private Mock<IMongoCollection<A>> _mockRootCollection;
        private Mock<IMongoCollection<B>> _mockDerivedCollection;
        private IMongoCollection<A> _rootCollection;
        private IMongoCollection<B> _derivedCollection;

        public OfTypeMongoCollectionTests()
        {
            _ofTypeFilter = new BsonDocument("_t", "B");
            _providedFilter = new BsonDocument("PropB", 4);
            _expectedFilter = new BsonDocument("_t", "B").Add("PropB", 4);

            _mockRootCollection = new Mock<IMongoCollection<A>>();
            _mockRootCollection.SetupGet(c => c.CollectionNamespace).Returns(CollectionNamespace.FromFullName("foo.bar"));
            _mockRootCollection.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
            _mockDerivedCollection = new Mock<IMongoCollection<B>>();
            _mockDerivedCollection.SetupGet(c => c.CollectionNamespace).Returns(CollectionNamespace.FromFullName("foo.bar"));
            _mockDerivedCollection.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());
            _rootCollection = _mockRootCollection.Object;
            _derivedCollection = _mockDerivedCollection.Object;
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_add_match_to_beginning_of_pipeline(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var options = new AggregateOptions();

            if (async)
            {
                subject.AggregateAsync<B>(new[] { new BsonDocument("$skip", 10) }, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.AggregateAsync(
                        It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", _ofTypeFilter))),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.Aggregate<B>(new[] { new BsonDocument("$skip", 10) }, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.Aggregate(
                        It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", _ofTypeFilter))),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_should_combine_match_statements_at_the_beginning_of_a_pipeline(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var options = new AggregateOptions();

            if (async)
            {
                subject.AggregateAsync<B>(new[] { new BsonDocument("$match", new BsonDocument("x", 1)) }, options, CancellationToken.None);

                var expectedFilter = new BsonDocument(_ofTypeFilter).Add("x", 1);
                _mockDerivedCollection.Verify(
                    c => c.AggregateAsync(
                        It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", expectedFilter))),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.Aggregate<B>(new[] { new BsonDocument("$match", new BsonDocument("x", 1)) }, options, CancellationToken.None);

                var expectedFilter = new BsonDocument(_ofTypeFilter).Add("x", 1);
                _mockDerivedCollection.Verify(
                    c => c.Aggregate(
                        It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", expectedFilter))),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void AggregateToCollection_should_add_match_to_beginning_of_pipeline(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var session = CreateSession(usingSession);
            var options = new AggregateOptions();

            if (async)
            {
                if (usingSession)
                {
                    subject.AggregateToCollectionAsync<B>(session, new[] { new BsonDocument("$skip", 10) }, options, CancellationToken.None);

                    _mockDerivedCollection.Verify(
                        c => c.AggregateToCollectionAsync(
                            session,
                            It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", _ofTypeFilter))),
                            options,
                            CancellationToken.None),
                        Times.Once);
                }
                else
                {
                    subject.AggregateToCollectionAsync<B>(new[] { new BsonDocument("$skip", 10) }, options, CancellationToken.None);

                    _mockDerivedCollection.Verify(
                        c => c.AggregateToCollectionAsync(
                            It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", _ofTypeFilter))),
                            options,
                            CancellationToken.None),
                        Times.Once);
                }
            }
            else
            {
                if (usingSession)
                {
                    subject.AggregateToCollection<B>(session, new[] { new BsonDocument("$skip", 10) }, options, CancellationToken.None);

                    _mockDerivedCollection.Verify(
                        c => c.AggregateToCollection(
                            session,
                            It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", _ofTypeFilter))),
                            options,
                            CancellationToken.None),
                        Times.Once);
                }
                else
                {
                    subject.AggregateToCollection<B>(new[] { new BsonDocument("$skip", 10) }, options, CancellationToken.None);

                    _mockDerivedCollection.Verify(
                        c => c.AggregateToCollection(
                            It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", _ofTypeFilter))),
                            options,
                            CancellationToken.None),
                        Times.Once);
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void AggregateToCollection_should_combine_match_statements_at_the_beginning_of_a_pipeline(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var options = new AggregateOptions();

            if (async)
            {
                subject.AggregateToCollectionAsync<B>(new[] { new BsonDocument("$match", new BsonDocument("x", 1)) }, options, CancellationToken.None);

                var expectedFilter = new BsonDocument(_ofTypeFilter).Add("x", 1);
                _mockDerivedCollection.Verify(
                    c => c.AggregateToCollectionAsync(
                        It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", expectedFilter))),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.AggregateToCollection<B>(new[] { new BsonDocument("$match", new BsonDocument("x", 1)) }, options, CancellationToken.None);

                var expectedFilter = new BsonDocument(_ofTypeFilter).Add("x", 1);
                _mockDerivedCollection.Verify(
                    c => c.AggregateToCollection(
                        It.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", expectedFilter))),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BulkWrite_with_DeleteOne(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var model = new DeleteOneModel<B>(_providedFilter)
            {
                Collation = new Collation("en_US"),
                Hint = new BsonDocument("_id", 1)
            };
            var options = new BulkWriteOptions();

            if (async)
            {
                subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.BulkWriteAsync(
                        It.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<DeleteOneModel<B>>()
                            .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter) &&
                                m.Collation == model.Collation &&
                                m.Hint == model.Hint).Count() == 1),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.BulkWrite(new[] { model }, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.BulkWrite(
                        It.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<DeleteOneModel<B>>()
                            .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter) &&
                                m.Collation == model.Collation &&
                                m.Hint == model.Hint).Count() == 1),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BulkWrite_with_DeleteMany(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var model = new DeleteManyModel<B>(_providedFilter)
            {
                Hint = new BsonDocument("_id", 1)
            };
            var options = new BulkWriteOptions();

            if (async)
            {
                subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.BulkWriteAsync(
                        It.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<DeleteManyModel<B>>()
                            .Where(m => m.Hint == model.Hint && RenderFilter(m.Filter).Equals(_expectedFilter)).Count() == 1),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.BulkWrite(new[] { model }, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.BulkWrite(
                        It.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<DeleteManyModel<B>>()
                            .Where(m => m.Hint == model.Hint && RenderFilter(m.Filter).Equals(_expectedFilter)).Count() == 1),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BulkWrite_with_ReplaceOne(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var replacement = new B();
            var model = new ReplaceOneModel<B>(_providedFilter, replacement)
            {
                Hint = new BsonDocument("_id", 1),
                IsUpsert = true
            };
            var options = new BulkWriteOptions();

            if (async)
            {
                subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.BulkWriteAsync(
                        It.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<ReplaceOneModel<B>>()
                            .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter) &&
                                m.Replacement == model.Replacement &&
                                m.Hint == model.Hint &&
                                m.IsUpsert == model.IsUpsert).Count() == 1),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.BulkWrite(new[] { model }, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.BulkWrite(
                        It.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<ReplaceOneModel<B>>()
                            .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter) &&
                                m.Replacement == model.Replacement &&
                                m.Hint == model.Hint &&
                                m.IsUpsert == model.IsUpsert).Count() == 1),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BulkWrite_with_UpdateMany<TRoot, TDerived>(
            [ClassValues(typeof(UpdateTestCases))] (TRoot, TDerived, string Discriminator) testCase,
            [Values(false, true)] bool isUpsert,
            [Values(false, true)] bool async)
                where TDerived : TRoot
        {
            var (mockDerivedCollection, subject) = CreateSubject<TRoot, TDerived>();
            var collation = new Collation("en_US");
            var model = new UpdateManyModel<TDerived>(_providedFilter, "{$set: {x: 1}}")
            {
                Collation = collation,
                Hint = new BsonDocument("_id", 1),
                IsUpsert = isUpsert
            };

            var options = new BulkWriteOptions();
            var updateString = isUpsert ?
                $"{{$set: {{x: 1}}, $setOnInsert: {{{testCase.Discriminator}}}}}" :
                "{$set: {x: 1}}";
            var expectedUpdate = BsonDocument.Parse(updateString);

            Func<UpdateManyModel<TDerived>, bool> isModelValid = m =>
                RenderFilter(m.Filter).Equals(_expectedFilter) &&
                RenderUpdate(m.Update).Equals(expectedUpdate) &&
                m.Collation == model.Collation &&
                m.Hint == model.Hint &&
                m.IsUpsert == model.IsUpsert;

            Expression<Func<IEnumerable<WriteModel<TDerived>>, bool>> modelMatch = v =>
                isModelValid(v.Single().As<UpdateManyModel<TDerived>>());

            if (async)
            {
                subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

                mockDerivedCollection.Verify(
                    c => c.BulkWriteAsync(
                        It.Is(modelMatch),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.BulkWrite(new[] { model }, options, CancellationToken.None);

                mockDerivedCollection.Verify(
                    c => c.BulkWrite(
                        It.Is(modelMatch),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BulkWrite_with_UpdateOne<TRoot, TDerived>(
            [ClassValues(typeof(UpdateTestCases))] (TRoot, TDerived, string Discriminator) testCase,
            [Values(false, true)] bool isUpsert,
            [Values(false, true)] bool async)
                where TDerived : TRoot
        {
            var (mockDerivedCollection, subject) = CreateSubject<TRoot, TDerived>();
            var collation = new Collation("en_US");
            var model = new UpdateOneModel<TDerived>(_providedFilter, "{$set: {x: 1}}")
            {
                Collation = collation,
                Hint = new BsonDocument("_id", 1),
                IsUpsert = isUpsert
            };
            var options = new BulkWriteOptions();
            var updateString = isUpsert ?
                $"{{$set: {{x: 1}}, $setOnInsert: {{{testCase.Discriminator}}}}}" :
                "{$set: {x: 1}}";
            var expectedUpdate = BsonDocument.Parse(updateString);

            Func<UpdateOneModel<TDerived>, bool> isModelValid = m =>
                RenderFilter(m.Filter).Equals(_expectedFilter) &&
                RenderUpdate(m.Update).Equals(expectedUpdate) &&
                m.Collation == model.Collation &&
                m.Hint == model.Hint &&
                m.IsUpsert == model.IsUpsert;

            Expression<Func<IEnumerable<WriteModel<TDerived>>, bool>> modelMatch = v =>
                isModelValid(v.Single().As<UpdateOneModel<TDerived>>());

            if (async)
            {
                subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

                mockDerivedCollection.Verify(
                    c => c.BulkWriteAsync(
                        It.Is(modelMatch),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.BulkWrite(new[] { model }, options, CancellationToken.None);

                mockDerivedCollection.Verify(
                    c => c.BulkWrite(
                        It.Is(modelMatch),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_should_include_the_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var options = new CountOptions();

            if (async)
            {
#pragma warning disable 618
                subject.CountAsync(_providedFilter, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.CountAsync(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        options,
                        CancellationToken.None),
                    Times.Once);
#pragma warning restore
            }
            else
            {
#pragma warning disable 618
                subject.Count(_providedFilter, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.Count(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        options,
                        CancellationToken.None),
                    Times.Once);
#pragma warning restore
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void CountDocuments_should_include_the_filter(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var session = CreateSession(usingSession);
            var options = new CountOptions();

            if (async)
            {
                if (usingSession)
                {
                    subject.CountDocumentsAsync(session, _providedFilter, options, CancellationToken.None);

                    _mockDerivedCollection.Verify(
                        c => c.CountDocumentsAsync(
                            session,
                            It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                            options,
                            CancellationToken.None),
                        Times.Once);
                }
                else
                {
                    subject.CountDocumentsAsync(_providedFilter, options, CancellationToken.None);

                    _mockDerivedCollection.Verify(
                        c => c.CountDocumentsAsync(
                            It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                            options,
                            CancellationToken.None),
                        Times.Once);
                }
            }
            else
            {
                if (usingSession)
                {
                    subject.CountDocuments(session, _providedFilter, options, CancellationToken.None);

                    _mockDerivedCollection.Verify(
                        c => c.CountDocuments(
                            session,
                            It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                            options,
                            CancellationToken.None),
                        Times.Once);
                }
                else
                {
                    subject.CountDocuments(_providedFilter, options, CancellationToken.None);

                    _mockDerivedCollection.Verify(
                        c => c.CountDocuments(
                            It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                            options,
                            CancellationToken.None),
                        Times.Once);
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Distinct_should_include_the_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var options = new DistinctOptions();

            if (async)
            {
                subject.DistinctAsync(x => x.PropA, _providedFilter, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.DistinctAsync(
                        It.Is<FieldDefinition<B, int>>(f => RenderField(f).Equals("PropA")),
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.Distinct(x => x.PropA, _providedFilter, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.Distinct(
                        It.Is<FieldDefinition<B, int>>(f => RenderField(f).Equals("PropA")),
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void EstimatedDocumentCount_should_throw(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    subject.EstimatedDocumentCountAsync(null, cancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    subject.EstimatedDocumentCount(null, cancellationToken);
                }

            });

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Find_should_include_the_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var options = new FindOptions<B>();

            if (async)
            {
                subject.FindAsync(_providedFilter, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.FindAsync(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.FindSync(_providedFilter, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.FindSync(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndDelete_should_include_the_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var options = new FindOneAndDeleteOptions<B>();

            if (async)
            {
                subject.FindOneAndDeleteAsync(_providedFilter, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.FindOneAndDeleteAsync(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.FindOneAndDelete(_providedFilter, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.FindOneAndDelete(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndReplace_should_include_the_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var replacement = new B();
            var options = new FindOneAndReplaceOptions<B>();

            if (async)
            {
                subject.FindOneAndReplaceAsync(_providedFilter, replacement, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.FindOneAndReplaceAsync(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        replacement,
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.FindOneAndReplace(_providedFilter, replacement, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.FindOneAndReplace(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        replacement,
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void FindOneAndUpdate_should_include_the_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var update = new BsonDocument("$set", new BsonDocument("x", 5));
            var options = new FindOneAndUpdateOptions<B>();

            if (async)
            {
                subject.FindOneAndUpdateAsync(_providedFilter, update, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.FindOneAndUpdateAsync(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        It.Is<UpdateDefinition<B>>(u => RenderUpdate(u).Equals(BsonDocument.Parse("{$set: {x: 5}}"))),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.FindOneAndUpdate(_providedFilter, update, options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.FindOneAndUpdate(
                        It.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                        It.Is<UpdateDefinition<B>>(u => RenderUpdate(u).Equals(BsonDocument.Parse("{$set: {x: 5}}"))),
                        options,
                        CancellationToken.None),
                    Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void MapReduce_should_include_the_filter_when_one_was_not_provided(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            if (async)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                subject.MapReduceAsync<B>("map", "reduce", null, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.MapReduceAsync(
                        "map",
                        "reduce",
                        It.Is<MapReduceOptions<B, B>>(o => RenderFilter(o.Filter).Equals(_ofTypeFilter)),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.MapReduce<B>("map", "reduce", null, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.MapReduce(
                        "map",
                        "reduce",
                        It.Is<MapReduceOptions<B, B>>(o => RenderFilter(o.Filter).Equals(_ofTypeFilter)),
                        CancellationToken.None),
                    Times.Once);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void MapReduce_should_include_the_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
#pragma warning disable CS0618 // Type or member is obsolete
            var options = new MapReduceOptions<B, B>
            {
                Filter = _providedFilter
            };

            if (async)
            {
                subject.MapReduceAsync("map", "reduce", options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.MapReduceAsync(
                        "map",
                        "reduce",
                        It.Is<MapReduceOptions<B, B>>(o => RenderFilter(o.Filter).Equals(_expectedFilter)),
                        CancellationToken.None),
                    Times.Once);
            }
            else
            {
                subject.MapReduce("map", "reduce", options, CancellationToken.None);

                _mockDerivedCollection.Verify(
                    c => c.MapReduce(
                        "map",
                        "reduce",
                        It.Is<MapReduceOptions<B, B>>(o => RenderFilter(o.Filter).Equals(_expectedFilter)),
                        CancellationToken.None),
                    Times.Once);
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void OfType_should_resort_to_root_collections_OfType()
        {
            var subject = CreateSubject();

            subject.OfType<C>();

            _mockRootCollection.Verify(c => c.OfType<C>(), Times.Once);
            _mockDerivedCollection.Verify(c => c.OfType<C>(), Times.Never);
        }

        // private methods
        private IClientSessionHandle CreateSession(bool usingSession)
        {
            if (usingSession)
            {
                var client = Mock.Of<IMongoClient>();
                var options = new ClientSessionOptions();
                var coreSession = Mock.Of<ICoreSession>();
                var coreServerSession = Mock.Of<ICoreServerSession>();
                Mock.Get(coreSession).SetupGet(m => m.ServerSession).Returns(coreServerSession);
                var coreSessionHandle = new CoreSessionHandle(coreSession);
                return new ClientSessionHandle(client, options, coreSessionHandle);
            }
            else
            {
                return null;
            }
        }

        private OfTypeMongoCollection<A, B> CreateSubject()
        {
            return new OfTypeMongoCollection<A, B>(_rootCollection, _derivedCollection, _ofTypeFilter);
        }

        private (Mock<IMongoCollection<TDerived>> MockDerivedCollection, OfTypeMongoCollection<TRoot, TDerived> Subject) CreateSubject<TRoot, TDerived>()
            where TDerived : TRoot
        {
            var mockRootCollection = new Mock<IMongoCollection<TRoot>>();
            mockRootCollection.SetupGet(c => c.CollectionNamespace).Returns(CollectionNamespace.FromFullName("foo.bar"));
            mockRootCollection.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());

            var mockDerivedCollection = new Mock<IMongoCollection<TDerived>>();
            mockDerivedCollection.SetupGet(c => c.CollectionNamespace).Returns(CollectionNamespace.FromFullName("foo.bar"));
            mockDerivedCollection.SetupGet(c => c.Settings).Returns(new MongoCollectionSettings());

            var subject = new OfTypeMongoCollection<TRoot, TDerived>(mockRootCollection.Object, mockDerivedCollection.Object, _ofTypeFilter);
            return (mockDerivedCollection, subject);
        }

        private string RenderField<TDocument, TField>(FieldDefinition<TDocument, TField> field)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            return field.Render(serializer, BsonSerializer.SerializerRegistry).FieldName;
        }

        private BsonDocument RenderFilter<TDocument>(FilterDefinition<TDocument> filter)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var doc = filter.Render(serializer, BsonSerializer.SerializerRegistry);
            return doc;
        }

        private List<BsonDocument> RenderPipeline<TInput, TOutput>(PipelineDefinition<TInput, TOutput> pipeline)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TInput>();
            return pipeline.Render(serializer, BsonSerializer.SerializerRegistry).Documents.ToList();
        }

        private BsonDocument RenderUpdate<TDocument>(UpdateDefinition<TDocument> update)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            return update.Render(serializer, BsonSerializer.SerializerRegistry).AsBsonDocument;
        }

        public class A
        {
            public int PropA;
        }

        public class B : A
        {
            public int PropB;
        }

        public class C : B
        {
            public int PropC;
        }

        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(BHierarchical), typeof(CHierarchical))]
        public class AHierarchical
        {
            public int PropA;
        }

        public class BHierarchical : AHierarchical
        {
            public int PropB;
        }

        public class CHierarchical : BHierarchical
        {
            public int PropC;
        }

        public class UpdateTestCases  : IValueGenerator
        {
            public object[] GenerateValues() => new object[]
                {
                    (new A(), new B(), "_t: \"B\""),
                    (new A(), new C(), "_t: \"C\""),
                    (new B(), new C(), "_t: \"C\""),
                    (new AHierarchical(), new BHierarchical(), "_t: [\"AHierarchical\", \"BHierarchical\"]"),
                    (new AHierarchical(), new CHierarchical(), "_t: [\"AHierarchical\", \"BHierarchical\", \"CHierarchical\"]"),
                    (new BHierarchical(), new CHierarchical(), "_t: [\"AHierarchical\", \"BHierarchical\", \"CHierarchical\"]")
                };
        }
    }

    public class OfTypeCollectionIntegrationTests : IDisposable
    {
        private readonly IMongoCollection<BsonDocument> _docsCollection;
        private readonly EventCapturer _eventsCapturer;
        private readonly IMongoCollection<A> _rootCollection;
        private readonly DisposableMongoClient _client;

        public OfTypeCollectionIntegrationTests()
        {
            var clientSettings = DriverTestConfiguration.Client.Settings.Clone();
            _eventsCapturer = new EventCapturer().Capture<CommandStartedEvent>();
            clientSettings.ClusterConfigurator = (b) => b.Subscribe(_eventsCapturer);
            _client = DriverTestConfiguration.CreateDisposableClient(clientSettings);

            var db = _client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            db.DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);

            _docsCollection = db.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            _rootCollection = db.GetCollection<A>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            var docs = new List<A>();
            docs.Add(new A { PropA = 1 });
            docs.Add(new A { PropA = 2 });
            docs.Add(new A { PropA = 3 });
            docs.Add(new B { PropA = 4, PropB = 1 });
            docs.Add(new B { PropA = 5, PropB = 2 });
            docs.Add(new B { PropA = 6, PropB = 3 });
            docs.Add(new C { PropA = 7, PropB = 4, PropC = 1 });
            docs.Add(new C { PropA = 8, PropB = 5, PropC = 2 });
            docs.Add(new C { PropA = 9, PropB = 6, PropC = 3 });

            _rootCollection.InsertMany(docs);
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_should_only_count_derived_types(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            long result1, result2;
            if (async)
            {
#pragma warning disable 618
                result1 = subject.CountAsync("{}").GetAwaiter().GetResult();
                result2 = subject.OfType<C>().CountAsync("{}").GetAwaiter().GetResult();
#pragma warning restore
            }
            else
            {
#pragma warning disable 618
                result1 = subject.Count("{}");
                result2 = subject.OfType<C>().Count("{}");
#pragma warning restore
            }

            result1.Should().Be(6);
            result2.Should().Be(3);
        }

        [Theory]
        [ParameterAttributeData]
        public void Count_should_only_count_derived_types_with_a_filter(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();

            long result;
            if (async)
            {
#pragma warning disable 618
                result = subject.CountAsync(x => x.PropB > 2).GetAwaiter().GetResult();
#pragma warning restore
            }
            else
            {
#pragma warning disable 618
                result = subject.Count(x => x.PropB > 2);
#pragma warning restore
            }

            result.Should().Be(4);
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertOne_should_include_discriminator_when_document_is_of_type_B(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var document = new B { PropA = 10, PropB = 7 };

            if (async)
            {
                subject.InsertOneAsync(document).GetAwaiter().GetResult();
            }
            else
            {
                subject.InsertOne(document);
            }

            var insertedB = _docsCollection.FindSync("{PropA: 10}").Single();
            insertedB["_t"].Should().Be(new BsonArray(new[] { "A", "B" }));
        }

        [Theory]
        [ParameterAttributeData]
        public void InsertOne_should_include_discriminator_when_document_is_of_type_C(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var document = new C { PropA = 11, PropB = 8, PropC = 4 };

            if (async)
            {
                subject.InsertOneAsync(document).GetAwaiter().GetResult();
            }
            else
            {
                subject.InsertOne(document);
            }

            var insertedC = _docsCollection.FindSync("{PropA: 11}").Single();
            insertedC["_t"].Should().Be(new BsonArray(new[] { "A", "B", "C" }));
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne_should_not_match_document_of_wrong_type(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var repacement = new B { PropA = 10, PropB = 7 };

            ReplaceOneResult result;
            if (async)
            {
                result = subject.ReplaceOneAsync("{PropA: 1}", repacement).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.ReplaceOne("{PropA: 1}", repacement);
            }

            result.MatchedCount.Should().Be(0); // document matching { PropA : 1 } is not of type B
        }

        [Theory]
        [ParameterAttributeData]
        public void ReplaceOne_should_match_document_of_right_type(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var originalDocument = _docsCollection.FindSync("{ PropA : 4 }").Single();
            var replacement = new B { PropA = 10, PropB = 7 };

            ReplaceOneResult result;
            if (async)
            {
                result = subject.ReplaceOneAsync("{PropA: 4}", replacement).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.ReplaceOne("{PropA: 4}", replacement);
            }

            result.MatchedCount.Should().Be(1); // document matching { PropA : 4 } is of type B
            var replacedB = _docsCollection.FindSync("{ PropA : 10 }").Single();
            replacedB.Should().Be(new BsonDocument
            {
                { "_id", originalDocument["_id"] },
                { "_t", new BsonArray { "A", "B" } },
                { "PropA", 10 },
                { "PropB", 7 }
            });
        }

        [Theory]
        [ParameterAttributeData]
        public void UpdateOne_should_match_document_of_right_type(
            [Values(false, true)] bool upsert,
            [Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.UpdateWithAggregationPipeline);

            string filter = "{ PropA : 4 }";

            var subject = CreateSubject();

            var pipeline = new EmptyPipelineDefinition<B>()
                .AppendStage<B, B, B>(
                    new BsonDocument
                    {
                        { "$set", new BsonDocument(nameof(B.PropA), "Pipeline") }
                    })
                .AppendStage<B, B, B>(
                    new BsonDocument
                    {
                        { "$unset", nameof(B.PropB) }
                    });

            var updateOptions = new UpdateOptions { IsUpsert = upsert };
            var result = async
                ? subject.UpdateOneAsync(filter, pipeline, updateOptions).GetAwaiter().GetResult()
                : subject.UpdateOne(filter, pipeline, updateOptions);

            result.MatchedCount.Should().Be(1);
            result.ModifiedCount.Should().Be(1);

            var updateQuery = _eventsCapturer.Events.OfType<CommandStartedEvent>().Last().Command["updates"];
            var expectedUpdateDocument = BsonDocument.Parse($@"
            {{
                ""q"" :
                {{
                    ""_t"" : ""B"",
                    ""PropA"" : 4
                }},
                ""u"" :
                [
                    {{
                        ""$set"" : {{ ""PropA"" : ""Pipeline"" }}
                    }},
                    {{ ""$unset"" : ""PropB"" }},
                    {{
                        ""$set"" :
                        {{
                            ""_t"" :
                            {{
                                ""$cond"" : [{{  ""$eq"" : [{{ ""$type"" : ""$_id"" }}, ""missing""] }},
                                [""A"", ""B""],
                                ""$_t""]
                            }}
                        }}
                    }}
                ],
                ""upsert"" : true
            }}");
            if (!upsert)
            {
                expectedUpdateDocument.RemoveAt(expectedUpdateDocument.ElementCount - 1); // remove upsert
                var uContent = expectedUpdateDocument["u"].AsBsonArray;
                uContent.RemoveAt(uContent.Count - 1);
            }
            updateQuery.Should().Be(new BsonArray { expectedUpdateDocument });
        }

        private IMongoCollection<B> CreateSubject()
        {
            return _rootCollection.OfType<B>();
        }

        public void Dispose() => _client?.Dispose();

        [BsonDiscriminator(RootClass = true)]
        [BsonKnownTypes(typeof(B), typeof(C))]
        public class A
        {
            public int PropA;
        }

        public class B : A
        {
            public int PropB;
        }

        public class C : B
        {
            public int PropC;
        }
    }
}
