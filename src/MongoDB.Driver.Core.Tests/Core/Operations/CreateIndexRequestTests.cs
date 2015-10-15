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
            subject.Name.Should().BeNull();
            subject.PartialFilterExpression.Should().BeNull();
            subject.Sparse.Should().NotHaveValue();
            subject.ExpireAfter.Should().NotHaveValue();
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
        public void CreateIndexDocument_should_return_expected_result_when_bits_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Bits = 20;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "bits", 20 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_bucketSize_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.BucketSize = 20;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "bucketSize", 20 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_defaultLanguage_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.DefaultLanguage = "es";
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "default_language", "es" }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_expireAfter_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.ExpireAfter = TimeSpan.FromSeconds(3);
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "expireAfterSeconds", 3 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_languageOverride_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.LanguageOverride = "en";
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "language_override", "en" }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_max_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Max = 20;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "max", 20 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_min_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Min = 20;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "min", 20 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_partialFilterExpression_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.PartialFilterExpression = new BsonDocument("x", new BsonDocument("$gt", 0));
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "partialFilterExpression", subject.PartialFilterExpression }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_sparse_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Sparse = true;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "sparse", true }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_sphereIndexVersion_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.SphereIndexVersion = 30;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "2dsphereIndexVersion", 30 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateCommand_should_return_expected_result_when_StorageEngine_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.StorageEngine = new BsonDocument("awesome", true);
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "storageEngine", new BsonDocument("awesome", true) }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_textIndexVersion_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.TextIndexVersion = 30;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "textIndexVersion", 30 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_unique_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Unique = true;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "unique", true }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_version_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Version = 11;
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "v", 11 }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_weights_has_value()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);
            subject.Weights = new BsonDocument("a", 1);
            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "weights", new BsonDocument("a", 1) }
            };

            var result = subject.CreateIndexDocument();

            result.Should().Be(expectedResult);
        }

        [Test]
        public void CreateIndexDocument_should_return_expected_result_when_name_is_in_additionalOptions()
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
        public void Name_get_and_set_should_work(
            [Values(null, "name")]
            string value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Name = value;
            var result = subject.Name;

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
        public void PartialFilterExpression_get_and_set_should_work()
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = new BsonDocument("x", new BsonDocument("$gt", 0));

            subject.PartialFilterExpression = value;
            var result = subject.PartialFilterExpression;

            result.Should().Be(value);
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

            subject.ExpireAfter = value;
            var result = subject.ExpireAfter;

            result.Should().Be(value);
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
