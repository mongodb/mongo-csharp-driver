/* Copyright 2010-2014 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq.Translators;
using MongoDB.Driver.Linq.Utils;
using NUnit.Framework;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Linq
{
    public class AggregateProjectionTranslatorTests_Project
    {
        private IMongoCollection<Root> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            var client = MongoDB.Driver.Tests.Configuration.TestClient;
            var db = client.GetDatabase(MongoDB.Driver.Tests.Configuration.TestDatabase.Name);
            _collection = db.GetCollection<Root>(MongoDB.Driver.Tests.Configuration.TestCollection.Name);
            db.DropCollectionAsync(_collection.CollectionNamespace.CollectionName).GetAwaiter().GetResult();

            var root = new Root
            {
                A = "Awesome",
                B = "Balloon",
                C = new C
                {
                    D = "Dexter",
                    E = new E
                    {
                        F = 11,
                        H = 22,
                        I = new[] { "it", "icky" }
                    }
                },
                G = new[] { 
                        new C
                        {
                            D = "Don't",
                            E = new E
                            {
                                F = 33,
                                H = 44,
                                I = new [] { "ignanimous"}
                            }
                        },
                        new C
                        {
                            D = "Dolphin",
                            E = new E
                            {
                                F = 55,
                                H = 66,
                                I = new [] { "insecure"}
                            }
                        }
                },
                Id = 10,
                J = new DateTime(2012, 12, 1, 13, 14, 15, 16),
                K = true
            };
            _collection.InsertOneAsync(root).GetAwaiter().GetResult();
        }

        [Test]
        public async Task Should_translate_add()
        {
            var result = await Project(x => new { Result = x.C.E.F + x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$add\": [\"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(33);
        }

        [Test]
        public async Task Should_translate_add_flattened()
        {
            var result = await Project(x => new { Result = x.Id + x.C.E.F + x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$add\": [\"$_id\", \"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(43);
        }

        [Test]
        public async Task Should_translate_and()
        {
            var result = await Project(x => new { Result = x.A == "yes" && x.B == "no" });

            result.Projection.Should().Be("{ Result: { \"$and\": [{ \"$eq\": [\"$A\", \"yes\"] }, { \"$eq\": [\"$B\", \"no\"] }] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Test]
        public async Task Should_translate_and_flattened()
        {
            var result = await Project(x => new { Result = x.A == "yes" && x.B == "no" && x.C.D == "maybe" });

            result.Projection.Should().Be("{ Result: { \"$and\": [{ \"$eq\": [\"$A\", \"yes\"] }, { \"$eq\": [\"$B\", \"no\"] }, { \"$eq\" : [\"$C.D\", \"maybe\"] } ] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Test]
        public async Task Should_translate_coalesce()
        {
            var result = await Project(x => new { Result = x.A ?? "funny" });

            result.Projection.Should().Be("{ Result: { \"$ifNull\": [\"$A\", \"funny\"] }, _id: 0 }");

            result.Value.Result.Should().Be("Awesome");
        }

        [Test]
        public async Task Should_translate_concat()
        {
            var result = await Project(x => new { Result = x.A + x.B });

            result.Projection.Should().Be("{ Result: { \"$concat\": [\"$A\", \"$B\"] }, _id: 0 }");

            result.Value.Result.Should().Be("AwesomeBalloon");
        }

        [Test]
        public async Task Should_translate_concat_flattened()
        {
            var result = await Project(x => new { Result = x.A + " " + x.B });

            result.Projection.Should().Be("{ Result: { \"$concat\": [\"$A\", \" \", \"$B\"] }, _id: 0 }");

            result.Value.Result.Should().Be("Awesome Balloon");
        }

        [Test]
        public async Task Should_translate_condition()
        {
            var result = await Project(x => new { Result = x.A == "funny" ? "a" : "b" });

            result.Projection.Should().Be("{ Result: { \"$cond\": [{ \"$eq\": [\"$A\", \"funny\"] }, \"a\", \"b\"] }, _id: 0 }");

            result.Value.Result.Should().Be("b");
        }

        [Test]
        public async Task Should_translate_day_of_month()
        {
            var result = await Project(x => new { Result = x.J.Day });

            result.Projection.Should().Be("{ Result: { \"$dayOfMonth\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(1);
        }

        [Test]
        public async Task Should_translate_day_of_week()
        {
            var result = await Project(x => new { Result = x.J.DayOfWeek });

            result.Projection.Should().Be("{ Result: { \"$subtract\" : [{ \"$dayOfWeek\": \"$J\" }, 1] }, _id: 0 }");

            result.Value.Result.Should().Be(DayOfWeek.Saturday);
        }

        [Test]
        public async Task Should_translate_day_of_year()
        {
            var result = await Project(x => new { Result = x.J.DayOfYear });

            result.Projection.Should().Be("{ Result: { \"$dayOfYear\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(336);
        }

        [Test]
        public async Task Should_translate_divide()
        {
            var result = await Project(x => new { Result = (double)x.C.E.F / x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$divide\": [\"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(0.5);
        }

        [Test]
        public async Task Should_translate_divide_3_numbers()
        {
            var result = await Project(x => new { Result = (double)x.Id / x.C.E.F / x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$divide\": [{ \"$divide\": [\"$_id\", \"$C.E.F\"] }, \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().BeApproximately(0.04, 3);
        }

        [Test]
        public async Task Should_translate_equals()
        {
            var result = await Project(x => new { Result = x.C.E.F == 5 });

            result.Projection.Should().Be("{ Result: { \"$eq\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Test]
        public async Task Should_translate_greater_than()
        {
            var result = await Project(x => new { Result = x.C.E.F > 5 });

            result.Projection.Should().Be("{ Result: { \"$gt\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Test]
        public async Task Should_translate_greater_than_or_equal()
        {
            var result = await Project(x => new { Result = x.C.E.F >= 5 });

            result.Projection.Should().Be("{ Result: { \"$gte\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Test]
        public async Task Should_translate_hour()
        {
            var result = await Project(x => new { Result = x.J.Hour });

            result.Projection.Should().Be("{ Result: { \"$hour\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(19);
        }

        [Test]
        public async Task Should_translate_less_than()
        {
            var result = await Project(x => new { Result = x.C.E.F < 5 });

            result.Projection.Should().Be("{ Result: { \"$lt\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Test]
        public async Task Should_translate_less_than_or_equal()
        {
            var result = await Project(x => new { Result = x.C.E.F <= 5 });

            result.Projection.Should().Be("{ Result: { \"$lte\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Test]
        public async Task Should_translate_millisecond()
        {
            var result = await Project(x => new { Result = x.J.Millisecond });

            result.Projection.Should().Be("{ Result: { \"$millisecond\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(16);
        }

        [Test]
        public async Task Should_translate_minute()
        {
            var result = await Project(x => new { Result = x.J.Minute });

            result.Projection.Should().Be("{ Result: { \"$minute\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(14);
        }

        [Test]
        public async Task Should_translate_modulo()
        {
            var result = await Project(x => new { Result = x.C.E.F % 5 });

            result.Projection.Should().Be("{ Result: { \"$mod\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().Be(1);
        }

        [Test]
        public async Task Should_translate_month()
        {
            var result = await Project(x => new { Result = x.J.Month });

            result.Projection.Should().Be("{ Result: { \"$month\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(12);
        }

        [Test]
        public async Task Should_translate_multiply()
        {
            var result = await Project(x => new { Result = x.C.E.F * x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$multiply\": [\"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(242);
        }

        [Test]
        public async Task Should_translate_multiply_flattened()
        {
            var result = await Project(x => new { Result = x.Id * x.C.E.F * x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$multiply\": [\"$_id\", \"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(2420);
        }

        [Test]
        public async Task Should_translate_not()
        {
            var result = await Project(x => new { Result = !x.K });

            result.Projection.Should().Be("{ Result: { \"$not\": \"$K\" }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Test]
        public async Task Should_translate_not_with_comparison()
        {
            var result = await Project(x => new { Result = !(x.C.E.F < 3) });

            result.Projection.Should().Be("{ Result: { \"$not\": [{ \"$lt\": [\"$C.E.F\", 3] }] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Test]
        public async Task Should_translate_not_equals()
        {
            var result = await Project(x => new { Result = x.C.E.F != 5 });

            result.Projection.Should().Be("{ Result: { \"$ne\": [\"$C.E.F\", 5] }, _id: 0 }");

            result.Value.Result.Should().BeTrue();
        }

        [Test]
        public async Task Should_translate_or()
        {
            var result = await Project(x => new { Result = x.A == "yes" || x.B == "no" });

            result.Projection.Should().Be("{ Result: { \"$or\": [{ \"$eq\": [\"$A\", \"yes\"] }, { \"$eq\": [\"$B\", \"no\"] }] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Test]
        public async Task Should_translate_or_flattened()
        {
            var result = await Project(x => new { Result = x.A == "yes" || x.B == "no" || x.C.D == "maybe" });

            result.Projection.Should().Be("{ Result: { \"$or\": [{ \"$eq\": [\"$A\", \"yes\"] }, { \"$eq\": [\"$B\", \"no\"] }, { \"$eq\" : [\"$C.D\", \"maybe\"] } ] }, _id: 0 }");

            result.Value.Result.Should().BeFalse();
        }

        [Test]
        public async Task Should_translate_second()
        {
            var result = await Project(x => new { Result = x.J.Second });

            result.Projection.Should().Be("{ Result: { \"$second\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(15);
        }

        [Test]
        public async Task Should_translate_substring()
        {
            var result = await Project(x => new { Result = x.B.Substring(3, 20) });

            result.Projection.Should().Be("{ Result: { \"$substr\": [\"$B\",3, 20] }, _id: 0 }");

            result.Value.Result.Should().Be("loon");
        }

        [Test]
        public async Task Should_translate_subtract()
        {
            var result = await Project(x => new { Result = x.C.E.F - x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$subtract\": [\"$C.E.F\", \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(-11);
        }

        [Test]
        public async Task Should_translate_subtract_3_numbers()
        {
            var result = await Project(x => new { Result = x.Id - x.C.E.F - x.C.E.H });

            result.Projection.Should().Be("{ Result: { \"$subtract\": [{ \"$subtract\": [\"$_id\", \"$C.E.F\"] }, \"$C.E.H\"] }, _id: 0 }");

            result.Value.Result.Should().Be(-23);
        }

        [Test]
        public async Task Should_translate_to_lower()
        {
            var result = await Project(x => new { Result = x.B.ToLower() });

            result.Projection.Should().Be("{ Result: { \"$toLower\": \"$B\" }, _id: 0 }");

            result.Value.Result.Should().Be("balloon");
        }

        [Test]
        public async Task Should_translate_to_lower_invariant()
        {
            var result = await Project(x => new { Result = x.B.ToLowerInvariant() });

            result.Projection.Should().Be("{ Result: { \"$toLower\": \"$B\" }, _id: 0 }");

            result.Value.Result.Should().Be("balloon");
        }

        [Test]
        public async Task Should_translate_to_upper()
        {
            var result = await Project(x => new { Result = x.B.ToUpper() });

            result.Projection.Should().Be("{ Result: { \"$toUpper\": \"$B\" }, _id: 0 }");

            result.Value.Result.Should().Be("BALLOON");
        }

        [Test]
        public async Task Should_translate_to_upper_invariant()
        {
            var result = await Project(x => new { Result = x.B.ToUpperInvariant() });

            result.Projection.Should().Be("{ Result: { \"$toUpper\": \"$B\" }, _id: 0 }");

            result.Value.Result.Should().Be("BALLOON");
        }

        [Test]
        public async Task Should_translate_year()
        {
            var result = await Project(x => new { Result = x.J.Year });

            result.Projection.Should().Be("{ Result: { \"$year\": \"$J\" }, _id: 0 }");

            result.Value.Result.Should().Be(2012);
        }

        [Test]
        public async Task Should_translate_array_projection()
        {
            var result = await Project(x => new { Result = x.G.Select(y => y.E.F) });

            result.Projection.Should().Be("{ Result: \"$G.E.F\", _id: 0 }");

            result.Value.Result.Should().BeEquivalentTo(33, 55);
        }

        [Test]
        [Ignore("MongoDB does something weird with this result. It returns F and H as two separate arrays, not an array of documents")]
        public async Task Should_translate_array_projection_complex()
        {
            var result = await Project(x => new { Result = x.G.Select(y => new { y.E.F, y.E.H }) });

            result.Projection.Should().Be("{ Result : { F : \"$G.E.F\", H : \"$G.E.H\" }, _id : 0 }");
        }

        private async Task<ProjectedResult<T>> Project<T>(Expression<Func<Root, T>> projector)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Root>();
            var projectionInfo = AggregateProjectionTranslator.TranslateProject<Root, T>(projector, serializer, BsonSerializer.SerializerRegistry);

            var pipelineOperator = new BsonDocument("$project", projectionInfo.Projection);
            var options = new AggregateOptions<T> { ResultSerializer = projectionInfo.Serializer };
            using (var cursor = await _collection.AggregateAsync<T>(new object[] { pipelineOperator }, options))
            {
                var list = await cursor.ToListAsync();
                return new ProjectedResult<T>
                {
                    Projection = projectionInfo.Projection,
                    Value = (T)list[0]
                };
            }
        }

        private class ProjectedResult<T>
        {
            public BsonDocument Projection;
            public T Value;
        }

        private class Root
        {
            public int Id { get; set; }

            public string A { get; set; }

            public string B { get; set; }

            public C C { get; set; }

            public IEnumerable<C> G { get; set; }

            public DateTime J { get; set; }

            public bool K { get; set; }
        }

        public class C
        {
            public string D { get; set; }

            public E E { get; set; }
        }

        public class E
        {
            public int F { get; set; }

            public int H { get; set; }

            public IEnumerable<string> I { get; set; }
        }
    }
}
