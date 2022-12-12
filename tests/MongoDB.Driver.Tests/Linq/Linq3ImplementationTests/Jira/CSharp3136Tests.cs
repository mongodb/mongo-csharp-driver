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
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3136Tests : Linq3IntegrationTest
    {

        [Fact]
        public void DateTime_ToString_with_no_arguments_should_work()
        {
            RequireServer.Check().Supports(Feature.ToConversionOperators);

            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => x.D.ToString());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { _v : { $toString : '$D' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal("2021-01-02T03:04:05.123Z", "2021-01-02T03:04:05.123Z");
        }

        [Fact]
        public void DateTime_ToString_with_format_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => x.D.ToString("%H:%M:%S"));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { _v : { $dateToString : { date : '$D', format : '%H:%M:%S' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal("03:04:05", "03:04:05");
        }

        [Theory]
        [InlineData(null, null, "{ $project : { _v : { $dateToString : { date : '$D' } }, _id : 0 } }", new[] { "2021-01-02T03:04:05.123Z", "2021-01-02T03:04:05.123Z" })]
        [InlineData("%H:%M:%S", null, "{ $project : { _v : { $dateToString : { date : '$D', format : '%H:%M:%S' } }, _id : 0 } }", new[] { "03:04:05", "03:04:05" })]
        [InlineData(null, "-04:00", "{ $project : { _v : { $dateToString : { date : '$D', timezone : '-04:00' } }, _id : 0 } }", new[] { "2021-01-01T23:04:05.123Z", "2021-01-01T23:04:05.123Z" })]
        [InlineData("%H:%M:%S", "-04:00", "{ $project : { _v : { $dateToString : { date : '$D', format : '%H:%M:%S', timezone : '-04:00' } }, _id : 0 } }", new[] { "23:04:05", "23:04:05" })]
        public void DateTime_ToString_with_format_and_timezone_constants_should_work(string format, string timezone, string expectedProjectStage, string[] expectedResults)
        {
            if (format == null)
            {
                RequireServer.Check().VersionGreaterThanOrEqualTo("4.0");
            }

            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => x.D.ToString(format, timezone));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                expectedProjectStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(false, false, "{ $project : { _v : { $dateToString : { date : '$D' } }, _id : 0 } }", new[] { "2021-01-02T03:04:05.123Z", "2021-01-02T03:04:05.123Z" })]
        [InlineData(true, false, "{ $project : { _v : { $dateToString : { date : '$D', format : '$Format' } }, _id : 0 } }", new[] { "03:04:05", "03:04:05" })]
        [InlineData(false, true, "{ $project : { _v : { $dateToString : { date : '$D', timezone : '$Timezone' } }, _id : 0 } }", new[] { "2021-01-01T23:04:05.123Z", "2021-01-01T23:04:05.123Z" })]
        [InlineData(true, true, "{ $project : { _v : { $dateToString : { date : '$D', format : '$Format', timezone : '$Timezone' } }, _id : 0 } }", new[] { "23:04:05", "23:04:05" })]
        public void DateTime_ToString_with_format_and_timezone_expressions_should_work(bool withFormat, bool withTimezone, string expectedProjectStage, string[] expectedResults)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("4.0");

            var collection = CreateCollection();

            var orderby = collection
                .AsQueryable()
                .OrderBy(x => x.Id);

            string @null = null; // null typed as string to match the desired overload
            var queryable = (withFormat, withTimezone) switch
            {
                (false, false) => orderby.Select(x => x.D.ToString(@null, @null)),
                (true, false) => orderby.Select(x => x.D.ToString(x.Format, @null)),
                (false, true) => orderby.Select(x => x.D.ToString(@null, x.Timezone)),
                (true, true) => orderby.Select(x => x.D.ToString(x.Format, x.Timezone))
            };

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                expectedProjectStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Fact]
        public void NullableDateTime_ToString_with_no_arguments_should_work()
        {
            RequireServer.Check().Supports(Feature.ToConversionOperators);

            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => x.N.ToString());

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { _v : { $toString : '$N' }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal("2021-01-02T03:04:05.123Z", null);
        }

        [Theory]
        [InlineData(null, null, null, "{ $project : { _v : { $dateToString : { date : '$N' } }, _id : 0 } }", new[] { "2021-01-02T03:04:05.123Z", null })]
        [InlineData(null, null, "xx", "{ $project : { _v : { $dateToString : { date : '$N', onNull : 'xx' } }, _id : 0 } }", new[] { "2021-01-02T03:04:05.123Z", "xx" })]
        [InlineData("%H:%M:%S", null, null, "{ $project : { _v : { $dateToString : { date : '$N', format : '%H:%M:%S' } }, _id : 0 } }", new[] { "03:04:05", null })]
        [InlineData("%H:%M:%S", null, "xx", "{ $project : { _v : { $dateToString : { date : '$N', format : '%H:%M:%S', onNull : 'xx' } }, _id : 0 } }", new[] { "03:04:05", "xx" })]
        [InlineData(null, "-04:00", null, "{ $project : { _v : { $dateToString : { date : '$N', timezone : '-04:00' } }, _id : 0 } }", new[] { "2021-01-01T23:04:05.123Z", null })]
        [InlineData(null, "-04:00", "xx", "{ $project : { _v : { $dateToString : { date : '$N', timezone : '-04:00', onNull : 'xx' } }, _id : 0 } }", new[] { "2021-01-01T23:04:05.123Z", "xx" })]
        [InlineData("%H:%M:%S", "-04:00", null, "{ $project : { _v : { $dateToString : { date : '$N', format : '%H:%M:%S', timezone : '-04:00' } }, _id : 0 } }", new[] { "23:04:05", null })]
        [InlineData("%H:%M:%S", "-04:00", "xx", "{ $project : { _v : { $dateToString : { date : '$N', format : '%H:%M:%S', timezone : '-04:00', onNull : 'xx' } }, _id : 0 } }", new[] { "23:04:05", "xx" })]
        public void NullableDateTime_ToString_with_format_and_timezone_and_onNull_constants_should_work(string format, string timezone, string onNull, string expectedProjectStage, string[] expectedResults)
        {
            if (format == null || onNull != null)
            {
                RequireServer.Check().VersionGreaterThanOrEqualTo("4.0");
            }

            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => x.N.ToString(format, timezone, onNull));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                expectedProjectStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(false, false, false, "{ $project : { _v : { $dateToString : { date : '$N' } }, _id : 0 } }", new[] { "2021-01-02T03:04:05.123Z", null })]
        [InlineData(false, false, true, "{ $project : { _v : { $dateToString : { date : '$N', onNull : '$OnNull' } }, _id : 0 } }", new[] { "2021-01-02T03:04:05.123Z", "missing" })]
        [InlineData(false, true, false, "{ $project : { _v : { $dateToString : { date : '$N', timezone : '$Timezone' } }, _id : 0 } }", new[] { "2021-01-01T23:04:05.123Z", null })]
        [InlineData(false, true, true, "{ $project : { _v : { $dateToString : { date : '$N', timezone : '$Timezone', onNull : '$OnNull' } }, _id : 0 } }", new[] { "2021-01-01T23:04:05.123Z", "missing" })]
        [InlineData(true, false, false, "{ $project : { _v : { $dateToString : { date : '$N', format : '$Format' } }, _id : 0 } }", new[] { "03:04:05", null })]
        [InlineData(true, false, true, "{ $project : { _v : { $dateToString : { date : '$N', format : '$Format', onNull : '$OnNull' } }, _id : 0 } }", new[] { "03:04:05", "missing" })]
        [InlineData(true, true, false, "{ $project : { _v : { $dateToString : { date : '$N', format : '$Format', timezone : '$Timezone' } }, _id : 0 } }", new[] { "23:04:05", null })]
        [InlineData(true, true, true, "{ $project : { _v : { $dateToString : { date : '$N', format : '$Format', timezone : '$Timezone', onNull : '$OnNull' } }, _id : 0 } }", new[] { "23:04:05", "missing" })]
        public void NullableDateTime_ToString_with_format_and_timezone_and_onNull_expressions_should_work(bool withFormat, bool withTimezone, bool withOnNull, string expectedProjectStage, string[] expectedResults)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("4.0");

            var collection = CreateCollection();

            var orderby = collection
                .AsQueryable()
                .OrderBy(x => x.Id);

            string @null = null; // null typed as string to match the desired overload
            var queryable = (withFormat, withTimezone, withOnNull) switch
            {
                (false, false, false) => orderby.Select(x => x.N.ToString(@null, @null, @null)),
                (false, false, true) => orderby.Select(x => x.N.ToString(@null, @null, x.OnNull)),
                (false, true, false) => orderby.Select(x => x.N.ToString(@null, x.Timezone, @null)),
                (false, true, true) => orderby.Select(x => x.N.ToString(@null, x.Timezone, x.OnNull)),
                (true, false, false) => orderby.Select(x => x.N.ToString(x.Format, @null, @null)),
                (true, false, true) => orderby.Select(x => x.N.ToString(x.Format, @null, x.OnNull)),
                (true, true, false) => orderby.Select(x => x.N.ToString(x.Format, x.Timezone, @null)),
                (true, true, true) => orderby.Select(x => x.N.ToString(x.Format, x.Timezone, x.OnNull))
            };

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                expectedProjectStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>();

            CreateCollection(
                collection,
                new C { Id = 1, D = new DateTime(2021, 1, 2, 3, 4, 5, 123, DateTimeKind.Utc), N = new DateTime(2021, 1, 2, 3, 4, 5, 123, DateTimeKind.Utc), Format = "%H:%M:%S", Timezone = "-04:00", OnNull = "missing" },
                new C { Id = 2, D = new DateTime(2021, 1, 2, 3, 4, 5, 123, DateTimeKind.Utc), N = null, Format = "%H:%M:%S", Timezone = "-04:00", OnNull = "missing" });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public DateTime D { get; set; }
            public DateTime? N { get; set; }
            public string Format { get; set; }
            public string Timezone { get; set; }
            public string OnNull { get; set; }
        }

        private class ProductTypeSearchResult
        {
            public bool IsExternalUrl { get; set; }
        }
    }
}
