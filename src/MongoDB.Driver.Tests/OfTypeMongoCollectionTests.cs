/* Copyright 2013-2014 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class OfTypeMongoCollectionTests
    {
        private BsonDocument _ofTypeFilter;
        private BsonDocument _expectedFilter;
        private BsonDocument _providedFilter;
        private IMongoCollection<A> _rootCollection;
        private IMongoCollection<B> _derivedCollection;

        [SetUp]
        public void SetUp()
        {
            _ofTypeFilter = new BsonDocument("_t", "B");
            _providedFilter = new BsonDocument("PropB", 4);
            _expectedFilter = new BsonDocument("_t", "B").Add("PropB", 4);

            _rootCollection = Substitute.For<IMongoCollection<A>>();
            _rootCollection.CollectionNamespace.Returns(CollectionNamespace.FromFullName("foo.bar"));
            _rootCollection.Settings.Returns(new MongoCollectionSettings());
            _derivedCollection = Substitute.For<IMongoCollection<B>>();
            _derivedCollection.CollectionNamespace.Returns(CollectionNamespace.FromFullName("foo.bar"));
            _derivedCollection.Settings.Returns(new MongoCollectionSettings());
        }

        [Test]
        public void AggregateAsync_should_add_match_to_beginning_of_pipeline()
        {
            var subject = CreateSubject();
            var options = new AggregateOptions();

            subject.AggregateAsync<B>(new[] { new BsonDocument("$skip", 10) }, options, CancellationToken.None);

            _derivedCollection.Received().AggregateAsync(
                Arg.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", _ofTypeFilter))),
                options,
                CancellationToken.None);
        }

        [Test]
        public void AggregateAsync_should_combine_match_statements_at_the_beginning_of_a_pipeline()
        {
            var subject = CreateSubject();
            var options = new AggregateOptions();

            subject.AggregateAsync<B>(new[] { new BsonDocument("$match", new BsonDocument("x", 1)) }, options, CancellationToken.None);

            var expectedFilter = new BsonDocument(_ofTypeFilter).Add("x", 1);
            _derivedCollection.Received().AggregateAsync(
                Arg.Is<PipelineDefinition<B, B>>(p => RenderPipeline(p)[0].Equals(new BsonDocument("$match", expectedFilter))),
                options,
                CancellationToken.None);
        }

        [Test]
        public void BulkWriteAsync_with_DeleteOne()
        {
            var subject = CreateSubject();
            var model = new DeleteOneModel<B>(_providedFilter);
            var options = new BulkWriteOptions();

            subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

            _derivedCollection.Received().BulkWriteAsync(
                Arg.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<DeleteOneModel<B>>()
                    .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter)).Count() == 1),
                options,
                CancellationToken.None);
        }

        [Test]
        public void BulkWriteAsync_with_DeleteMany()
        {
            var subject = CreateSubject();
            var model = new DeleteManyModel<B>(_providedFilter);
            var options = new BulkWriteOptions();

            subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

            _derivedCollection.Received().BulkWriteAsync(
                Arg.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<DeleteManyModel<B>>()
                    .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter)).Count() == 1),
                options,
                CancellationToken.None);
        }

        [Test]
        public void BulkWriteAsync_with_ReplaceOne()
        {
            var subject = CreateSubject();
            var replacement = new B();
            var model = new ReplaceOneModel<B>(_providedFilter, replacement) { IsUpsert = true };
            var options = new BulkWriteOptions();

            subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

            _derivedCollection.Received().BulkWriteAsync(
                Arg.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<ReplaceOneModel<B>>()
                    .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter) &&
                        m.Replacement == model.Replacement &&
                        m.IsUpsert == model.IsUpsert).Count() == 1),
                options,
                CancellationToken.None);
        }

        [Test]
        public void BulkWriteAsync_with_UpdateMany()
        {
            var subject = CreateSubject();
            var replacement = new B();
            var model = new UpdateManyModel<B>(_providedFilter, "{$set: {x: 1}}") { IsUpsert = true };
            var options = new BulkWriteOptions();

            subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

            _derivedCollection.Received().BulkWriteAsync(
                Arg.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<UpdateManyModel<B>>()
                    .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter) &&
                        RenderUpdate(m.Update).Equals(BsonDocument.Parse("{$set: {x: 1}}")) &&
                        m.IsUpsert == model.IsUpsert).Count() == 1),
                options,
                CancellationToken.None);
        }

        [Test]
        public void BulkWriteAsync_with_UpdateOne()
        {
            var subject = CreateSubject();
            var replacement = new B();
            var model = new UpdateOneModel<B>(_providedFilter, "{$set: {x: 1}}") { IsUpsert = true };
            var options = new BulkWriteOptions();

            subject.BulkWriteAsync(new[] { model }, options, CancellationToken.None);

            _derivedCollection.Received().BulkWriteAsync(
                Arg.Is<IEnumerable<WriteModel<B>>>(v => v.OfType<UpdateOneModel<B>>()
                    .Where(m => RenderFilter(m.Filter).Equals(_expectedFilter) &&
                        RenderUpdate(m.Update).Equals(BsonDocument.Parse("{$set: {x: 1}}")) &&
                        m.IsUpsert == model.IsUpsert).Count() == 1),
                options,
                CancellationToken.None);
        }

        [Test]
        public void CountAsync_should_include_the_filter()
        {
            var subject = CreateSubject();
            var options = new CountOptions();

            subject.CountAsync(_providedFilter, options, CancellationToken.None);

            _derivedCollection.Received().CountAsync(
                Arg.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                options,
                CancellationToken.None);
        }

        [Test]
        public void DistinctAsync_should_include_the_filter()
        {
            var subject = CreateSubject();
            var options = new DistinctOptions();

            subject.DistinctAsync(x => x.PropA, _providedFilter, options, CancellationToken.None);

            _derivedCollection.Received().DistinctAsync(
                Arg.Is<FieldDefinition<B, int>>(f => RenderField(f).Equals("PropA")),
                Arg.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                options,
                CancellationToken.None);
        }

        [Test]
        public void FindAsync_should_include_the_filter()
        {
            var subject = CreateSubject();
            var options = new FindOptions<B>();

            subject.FindAsync(_providedFilter, options, CancellationToken.None);

            _derivedCollection.Received().FindAsync(
                Arg.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                options,
                CancellationToken.None);
        }

        [Test]
        public void FindOneAndDeleteAsync_should_include_the_filter()
        {
            var subject = CreateSubject();
            var options = new FindOneAndDeleteOptions<B>();

            subject.FindOneAndDeleteAsync(_providedFilter, options, CancellationToken.None);

            _derivedCollection.Received().FindOneAndDeleteAsync(
                Arg.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                options,
                CancellationToken.None);
        }

        [Test]
        public void FindOneAndReplaceAsync_should_include_the_filter()
        {
            var subject = CreateSubject();
            var replacement = new B();
            var options = new FindOneAndReplaceOptions<B>();

            subject.FindOneAndReplaceAsync(_providedFilter, replacement, options, CancellationToken.None);

            _derivedCollection.Received().FindOneAndReplaceAsync(
                Arg.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                replacement,
                options,
                CancellationToken.None);
        }

        [Test]
        public void FindOneAndUpdateAsync_should_include_the_filter()
        {
            var subject = CreateSubject();
            var update = new BsonDocument("$set", new BsonDocument("x", 5));
            var options = new FindOneAndUpdateOptions<B>();

            subject.FindOneAndUpdateAsync(_providedFilter, update, options, CancellationToken.None);

            _derivedCollection.Received().FindOneAndUpdateAsync(
                Arg.Is<FilterDefinition<B>>(f => RenderFilter(f).Equals(_expectedFilter)),
                Arg.Is<UpdateDefinition<B>>(u => RenderUpdate(u).Equals(BsonDocument.Parse("{$set: {x: 5}}"))),
                options,
                CancellationToken.None);
        }

        [Test]
        public void MapReduceAsync_should_include_the_filter_when_one_was_not_provided()
        {
            var subject = CreateSubject();

            subject.MapReduceAsync<B>("map", "reduce", null, CancellationToken.None);

            _derivedCollection.Received().MapReduceAsync(
                "map",
                "reduce",
                Arg.Is<MapReduceOptions<B, B>>(o => RenderFilter(o.Filter).Equals(_ofTypeFilter)),
                CancellationToken.None);
        }

        [Test]
        public void MapReduceAsync_should_include_the_filter()
        {
            var subject = CreateSubject();
            var options = new MapReduceOptions<B, B>
            {
                Filter = _providedFilter
            };

            subject.MapReduceAsync("map", "reduce", options, CancellationToken.None);

            _derivedCollection.Received().MapReduceAsync(
                "map",
                "reduce",
                Arg.Is<MapReduceOptions<B, B>>(o => RenderFilter(o.Filter).Equals(_expectedFilter)),
                CancellationToken.None);
        }

        [Test]
        public void OfType_should_resort_to_root_collections_OfType()
        {
            var subject = CreateSubject();

            var result = subject.OfType<C>();

            _rootCollection.Received().OfType<C>();
            _derivedCollection.DidNotReceive().OfType<C>();
        }

        private OfTypeMongoCollection<A, B> CreateSubject()
        {
            return new OfTypeMongoCollection<A, B>(_rootCollection, _derivedCollection, _ofTypeFilter);
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
            return update.Render(serializer, BsonSerializer.SerializerRegistry);
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
    }

    [TestFixture]
    public class OfTypeCollectionIntegrationTests
    {
        private IMongoCollection<BsonDocument> _docsCollection;
        private IMongoCollection<A> _rootCollection;

        [SetUp]
        public void SetUp()
        {
            var client = DriverTestConfiguration.Client;
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            db.DropCollectionAsync(DriverTestConfiguration.CollectionNamespace.CollectionName).GetAwaiter().GetResult();

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

            _rootCollection.InsertManyAsync(docs).GetAwaiter().GetResult();
        }

        [Test]
        public async Task CountAsync_should_only_count_derived_types()
        {
            var subject = CreateSubject();

            var result1 = await subject.CountAsync("{}");
            var result2 = await subject.OfType<C>().CountAsync("{}");

            result1.Should().Be(6);
            result2.Should().Be(3);
        }

        [Test]
        public async Task CountAsync_should_only_count_derived_types_with_a_filter()
        {
            var subject = CreateSubject();

            var result = await subject.CountAsync(x => x.PropB > 2);

            result.Should().Be(4);
        }

        [Test]
        public async Task InsertOneAsync_should_include_discriminator_when_document_is_of_type_B()
        {
            var subject = CreateSubject();

            await subject.InsertOneAsync(new B { PropA = 10, PropB = 7 });

            var insertedB = await _docsCollection.Find("{PropA: 10}").SingleAsync();
            insertedB["_t"].Should().Be(new BsonArray(new[] { "A", "B" }));
        }

        [Test]
        public async Task InsertOneAsync_should_include_discriminator_when_document_is_of_type_C()
        {
            var subject = CreateSubject();

            await subject.InsertOneAsync(new C { PropA = 11, PropB = 8, PropC = 4 });

            var insertedC = await _docsCollection.Find("{PropA: 11}").SingleAsync();
            insertedC["_t"].Should().Be(new BsonArray(new[] { "A", "B", "C" }));
        }

        [Test]
        public async Task ReplaceOneAsync_should_not_match_document_of_wrong_type()
        {
            var subject = CreateSubject();

            var result = await subject.ReplaceOneAsync("{PropA: 1}", new B { PropA = 10, PropB = 7 });

            result.MatchedCount.Should().Be(0); // document matching { PropA : 1 } is not of type B
        }

        [Test]
        public async Task ReplaceOneAsync_should_match_document_of_right_type()
        {
            var subject = CreateSubject();
            var originalDocument = await _docsCollection.Find("{ PropA : 4 }").SingleAsync();

            var result = await subject.ReplaceOneAsync("{PropA: 4}", new B { PropA = 10, PropB = 7 });

            result.MatchedCount.Should().Be(1); // document matching { PropA : 4 } is of type B
            var replacedB = await _docsCollection.Find("{ PropA : 10 }").SingleAsync();
            replacedB.Should().Be(new BsonDocument
            {
                { "_id", originalDocument["_id"] },
                { "_t", new BsonArray { "A", "B" } },
                { "PropA", 10 },
                { "PropB", 7 }
            });
        }

        private IMongoCollection<B> CreateSubject()
        {
            return _rootCollection.OfType<B>();
        }

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