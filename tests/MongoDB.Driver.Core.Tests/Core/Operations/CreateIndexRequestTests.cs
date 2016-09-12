/* Copyright 2013-2016 MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class CreateIndexRequestTests
    {
        [Theory]
        [ParameterAttributeData]
        public void AdditionalOptions_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.AdditionalOptions = value;
            var result = subject.AdditionalOptions;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Background_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Background = value;
            var result = subject.Background;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Bits_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Bits = value;
            var result = subject.Bits;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void BucketSize_get_and_set_should_work(
            [Values(null, 1.0, 2.0)]
            double? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.BucketSize = value;
            var result = subject.BucketSize;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Collation_get_and_set_should_work(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = locale == null ? null : new Collation(locale);

            subject.Collation = value;
            var result = subject.Collation;

            result.Should().BeSameAs(value);
        }

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var keys = new BsonDocument("x", 1);

            var subject = new CreateIndexRequest(keys);

            subject.Keys.Should().BeSameAs(keys);

            subject.AdditionalOptions.Should().BeNull();
            subject.Background.Should().NotHaveValue();
            subject.Bits.Should().NotHaveValue();
            subject.BucketSize.Should().NotHaveValue();
            subject.Collation.Should().BeNull();
            subject.DefaultLanguage.Should().BeNull();
            subject.ExpireAfter.Should().NotHaveValue();
            subject.LanguageOverride.Should().BeNull();
            subject.Max.Should().NotHaveValue();
            subject.Min.Should().NotHaveValue();
            subject.Name.Should().BeNull();
            subject.PartialFilterExpression.Should().BeNull();
            subject.Sparse.Should().NotHaveValue();
            subject.SphereIndexVersion.Should().NotHaveValue();
            subject.TextIndexVersion.Should().NotHaveValue();
            subject.Unique.Should().NotHaveValue();
            subject.Version.Should().NotHaveValue();
            subject.Weights.Should().BeNull();
        }

        [Fact]
        public void constructor_should_throw_when_keys_is_null()
        {
            var exception = Record.Exception(() => new CreateIndexRequest(null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("keys");
        }

        [Fact]
        public void CreateIndexDocument_should_return_expected_result()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys);

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_AdditionalOptions_is_set(
            [Values(null, "{ x : 123 }", "{ x : 123, sparse : false }")]
            string additionalOptionsString)
        {
            var keys = new BsonDocument("x", 1);
            var additionalOptions = additionalOptionsString == null ? null : BsonDocument.Parse(additionalOptionsString);
            var subject = new CreateIndexRequest(keys)
            {
                Sparse = true,
                AdditionalOptions = additionalOptions
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "sparse", true }, // should not be overwritten by additionalOptions
                { "x", 123, additionalOptions != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Background_is_set(
            [Values(null, false, true)]
            bool? background)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                Background = background
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "background", () => background.Value, background.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Bits_is_set(
            [Values(null, 1, 2)]
            int? bits)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                Bits = bits
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "bits", () => bits.Value, bits.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_BucketSize_is_set(
            [Values(null, 1.0, 2.0)]
            double? bucketSize)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                BucketSize = bucketSize
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "bucketSize", () => bucketSize.Value, bucketSize.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Collation_is_set(
            [Values(null, "en_US", "fr_CA")]
            string locale)
        {
            var keys = new BsonDocument("x", 1);
            var collation = locale == null ? null : new Collation(locale);
            var subject = new CreateIndexRequest(keys)
            {
                Collation = collation
            };

            var result = subject.CreateIndexDocument(Feature.Collation.FirstSupportedVersion);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "collation", () => collation.ToBsonDocument(), collation != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_DefaultLanguage_is_set(
            [Values(null, "en", "fr")]
            string defaultLanguage)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                DefaultLanguage = defaultLanguage
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "default_language", defaultLanguage, defaultLanguage != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_ExpireAfter_is_set(
            [Values(null, 1, 2)]
            int? seconds)
        {
            var keys = new BsonDocument("x", 1);
            var expireAfter = seconds == null ? (TimeSpan?)null : TimeSpan.FromSeconds(seconds.Value);
            var subject = new CreateIndexRequest(keys)
            {
                ExpireAfter = expireAfter
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "expireAfterSeconds", () => expireAfter.Value.TotalSeconds, expireAfter.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_LanguageOverride_is_set(
            [Values(null, "en", "fr")]
            string languageOverride)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                LanguageOverride = languageOverride
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "language_override", () => languageOverride, languageOverride != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Max_is_set(
            [Values(null, 1.0, 2.0)]
            double? max)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                Max = max
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "max", () => max.Value, max.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Min_is_set(
            [Values(null, 1.0, 2.0)]
            double? min)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                Min = min
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "min", () => min.Value, min.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Name_is_set(
            [Values(null, "a", "b")]
            string name,
            [Values(null, "{ name : 'x' }", "{ name : 'y' }")]
            string additionalOptionsString)
        {
            var keys = new BsonDocument("x", 1);
            var additionalOptions = additionalOptionsString == null ? null : BsonDocument.Parse(additionalOptionsString);
            var subject = new CreateIndexRequest(keys)
            {
                Name = name,
                AdditionalOptions = additionalOptions // secondary source of name
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", name != null ? name : additionalOptions != null ? additionalOptions["name"].AsString : "x_1" },
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_PartialFilterExpression_is_set(
            [Values(null, "{ x : { $gt : 1 } }", "{ x : { $gt : 2 } }")]
            string partialFilterExpressionString)
        {
            var keys = new BsonDocument("x", 1);
            var partialFilterExpression = partialFilterExpressionString == null ? null : BsonDocument.Parse(partialFilterExpressionString);
            var subject = new CreateIndexRequest(keys)
            {
                PartialFilterExpression = partialFilterExpression
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "partialFilterExpression", partialFilterExpression, partialFilterExpression != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Sparse_is_set(
            [Values(null, false, true)]
            bool? sparse)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                Sparse = sparse
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "sparse", () => sparse.Value, sparse.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_SphereIndexVersion_is_set(
            [Values(null, 1, 2)]
            int? sphereIndexVersion)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                SphereIndexVersion = sphereIndexVersion
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "2dsphereIndexVersion", () => sphereIndexVersion.Value, sphereIndexVersion.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_StorageEngine_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string storageEngineString)
        {
            var keys = new BsonDocument("x", 1);
            var storageEngine = storageEngineString == null ? null : BsonDocument.Parse(storageEngineString);
            var subject = new CreateIndexRequest(keys)
            {
                StorageEngine = storageEngine
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "storageEngine", storageEngine, storageEngine != null }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_TextIndexVersion_is_set(
            [Values(null, 1, 2)]
            int? textIndexVersion)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                TextIndexVersion = textIndexVersion
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "textIndexVersion", () => textIndexVersion.Value, textIndexVersion.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Unique_is_set(
            [Values(null, false, true)]
            bool? unique)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                Unique = unique
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "unique", () => unique.Value, unique.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Version_is_set(
            [Values(null, 1, 2)]
            int? version)
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                Version = version
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "v", () => version.Value, version.HasValue }
            };
            result.Should().Be(expectedResult);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIndexDocument_should_return_expected_result_when_Weights_is_set(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string weightsString)
        {
            var keys = new BsonDocument("x", 1);
            var weights = weightsString == null ? null : BsonDocument.Parse(weightsString);
            var subject = new CreateIndexRequest(keys)
            {
                Weights = weights
            };

            var result = subject.CreateIndexDocument(null);

            var expectedResult = new BsonDocument
            {
                { "key", keys },
                { "name", "x_1" },
                { "weights", weights, weights != null }
            };
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CreateIndexDocument_should_throw_when_Collation__is_set_and_is_not_supported()
        {
            var keys = new BsonDocument("x", 1);
            var subject = new CreateIndexRequest(keys)
            {
                Collation = new Collation("en_US")
            };

            var exception = Record.Exception(() => subject.CreateIndexDocument(Feature.Collation.LastNotSupportedVersion));

            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void DefaultLanguage_get_and_set_should_work(
            [Values(null, "x", "y")]
            string value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.DefaultLanguage = value;
            var result = subject.DefaultLanguage;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void ExpireAfter_get_and_set_should_work(
            [Values(null, 1L, 2L)]
            long? milliseconds)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = milliseconds == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(milliseconds.Value);

            subject.ExpireAfter = value;
            var result = subject.ExpireAfter;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void LanguageOverride_get_and_set_should_work(
            [Values(null, "en", "fr")]
            string value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.LanguageOverride = value;
            var result = subject.LanguageOverride;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Max_get_and_set_should_work(
            [Values(null, 1.0, 2.0)]
            double? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Max = value;
            var result = subject.Max;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Min_get_and_set_should_work(
            [Values(null, 1.0, 2.0)]
            double? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Min = value;
            var result = subject.Min;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Name_get_and_set_should_work(
            [Values(null, "name")]
            string value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Name = value;
            var result = subject.Name;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void PartialFilterExpression_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.PartialFilterExpression = value;
            var result = subject.PartialFilterExpression;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Sparse_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Sparse = value;
            var result = subject.Sparse;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void SphereIndexVersion_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.SphereIndexVersion = value;
            var result = subject.SphereIndexVersion;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void StorageEngine_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.StorageEngine = value;
            var result = subject.StorageEngine;

            result.Should().BeSameAs(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void TextIndexVersion_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.TextIndexVersion = value;
            var result = subject.TextIndexVersion;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Unique_get_and_set_should_work(
            [Values(null, false, true)]
            bool? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Unique = value;
            var result = subject.Unique;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Version_get_and_set_should_work(
            [Values(null, 1, 2)]
            int? value)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));

            subject.Version = value;
            var result = subject.Version;

            result.Should().Be(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Weights_get_and_set_should_work(
            [Values(null, "{ x : 1 }", "{ x : 2 }")]
            string valueString)
        {
            var subject = new CreateIndexRequest(new BsonDocument("x", 1));
            var value = valueString == null ? null : BsonDocument.Parse(valueString);

            subject.Weights = value;
            var result = subject.Weights;

            result.Should().BeSameAs(value);
        }
    }
}
