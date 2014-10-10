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
using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class CreateIndexRequestTests
    {
        [Test]
        public void AdditionalOptions_get_and_set_should_work(
            [Values(false, true)]
            bool hasAdditionalOptions)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = hasAdditionalOptions ? new BsonDocument("y", 2) : null;

            subject.AdditionalOptions = value;
            var result = subject.AdditionalOptions;

            result.Should().Be(value);
        }

        [Test]
        public void Background_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Background = value;
            var result = subject.Background;

            result.Should().Be(value);
        }

        [Test]
        public void constructor_should_initialize_subject()
        {
            var keys = new BsonDocument("x", 1);

            var subject = new CreateIndexRequest(keys);

            subject.Keys.Should().BeSameAs(keys);
            subject.AdditionalOptions.Should().BeNull();
            subject.Background.Should().NotHaveValue();
            subject.IndexName.Should().BeNull();
            subject.Sparse.Should().NotHaveValue();
            subject.TimeToLive.Should().NotHaveValue();
            subject.Unique.Should().NotHaveValue();
        }

        [Test]
        public void constructor_should_throw_when_keys_is_null()
        {
            Action action = () => new CreateIndexRequest(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("keys");
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_additionalOptions_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Sparse = true;
            subject.AdditionalOptions = new BsonDocument
            {
                { "sparse", false }, // should not overwrite existing element
                { "x", 123 }
            };
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "sparse", true },
                { "x", 123 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_background_has_value(
            [Values(false, true)]
            bool value)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Background = value;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "background", value }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_indexName_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.IndexName = "i";
            subject.AdditionalOptions = new BsonDocument("name", "a"); // IndexName takes precedence
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "i" }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_indexName_is_in_additionalOptions()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.AdditionalOptions = new BsonDocument("name", "a");
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "a" }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_sparse_has_value(
            [Values(false, true)]
            bool value)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Sparse = value;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "sparse", value }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_timeToLive_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.TimeToLive = TimeSpan.FromSeconds(1);
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "expireAfterSeconds", 1 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_unique_has_value(
            [Values(false, true)]
            bool value)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Unique = value;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "unique", value }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void IndexName_get_and_set_should_work(
            [Values(null, "name")]
            string value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.IndexName = value;
            var result = subject.IndexName;

            result.Should().Be(value);
        }

        [Test]
        public void Keys_get_should_return_expected_result()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);

            var result = subject.Keys;

            result.Should().Be(keys);
        }

        [Test]
        public void Sparse_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Sparse = value;
            var result = subject.Sparse;

            result.Should().Be(value);
        }

        [Test]
        public void TimeToLive_get_and_set_should_work(
            [Values(null, 1)]
            int? seconds)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = seconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(seconds.Value) : null;

            subject.TimeToLive = value;
            var result = subject.TimeToLive;

            result.Should().Be(value);
        }

        [Test]
        public void TimeToLive_set_should_throw_when_value_is_not_valid(
            [Values(-1, 0)]
            int seconds)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = TimeSpan.FromSeconds(seconds);

            Action action = () => { subject.TimeToLive = value; };

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("value");
        }

        [Test]
        public void Unique_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Unique = value;
            var result = subject.Unique;

            result.Should().Be(value);
        }
    }
}
