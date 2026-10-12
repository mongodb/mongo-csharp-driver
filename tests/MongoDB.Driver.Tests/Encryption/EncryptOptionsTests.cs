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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Encryption;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption
{
    public class EncryptOptionsTests
    {
        [Fact]
        public void Constructor_should_fail_when_algorithm_is_null()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_fail_when_contentionFactor_and_algorithm_is_not_indexed()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: "test", contentionFactor: 1, keyId: Guid.NewGuid()));
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().Be("ContentionFactor only applies for Indexed, Range, or String algorithm.");
        }

        [Fact]
        public void Constructor_should_fail_when_keyId_and_alternateKeyName_are_both_empty()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: "test", alternateKeyName: null, keyId: null));
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().Be("Key Id and AlternateKeyName may not both be null.");
        }

        [Fact]
        public void Constructor_should_fail_when_keyId_and_alternateKeyName_are_both_specified()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: "test", alternateKeyName: "alternateKeyName", keyId: Guid.NewGuid()));
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().Be("Key Id and AlternateKeyName may not both be set.");
        }

        [Fact]
        public void Constructor_should_fail_when_queryType_and_algorithm_is_not_indexed()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: "test", queryType: "equality", keyId: Guid.NewGuid()));
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().Be("QueryType only applies for Indexed, Range, or String algorithm.");
        }

        [Fact]
        public void Constructor_should_fail_when_rangeOptions_and_algorithm_is_not_range()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: "test", keyId: Guid.NewGuid(), rangeOptions: new RangeOptions()));
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().Be("RangeOptions only applies for Range algorithm.");
        }

        [Fact]
        public void Constructor_should_fail_when_stringOptions_is_null()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: "test", stringOptions: null));
            exception.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("stringOptions");
        }

        [Fact]
        public void Constructor_should_fail_when_stringOptions_and_algorithm_is_not_String()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: "test", keyId: Guid.NewGuid(), stringOptions: new StringOptions(true, true)));

            exception.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Be("StringOptions only applies for String algorithm.");
        }

        [Fact]
        public void Constructor_should_fail_with_invalid_queryType_for_String()
        {
            var invalidQueryType = "equality";

            var exception = Record.Exception(() => new EncryptOptions(algorithm: EncryptionAlgorithm.String, keyId: Guid.NewGuid(), queryType: invalidQueryType));

            exception.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain($"QueryType '{invalidQueryType}' is not valid for String algorithm");
        }

        [Theory]
        [InlineData("prefix")]
        [InlineData("prefixPreview")]
        [InlineData("suffix")]
        [InlineData("suffixPreview")]
        [InlineData("substring")]
        [InlineData("substringPreview")]
        public void Constructor_should_succeed_with_valid_queryType_for_String(string validQueryType)
        {
            var subject = new EncryptOptions(algorithm: EncryptionAlgorithm.String, keyId: Guid.NewGuid(), queryType: validQueryType);

            subject.QueryType.Should().Be(validQueryType);
        }

        [Fact]
        public void Constructor_should_fail_when_prefix_queryType_without_prefixOptions()
        {
            var exception = Record.Exception(() => new EncryptOptions(
                algorithm: EncryptionAlgorithm.String,
                keyId: Guid.NewGuid(),
                queryType: "prefix",
                stringOptions: new StringOptions(true, true)));

            exception.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("PrefixOptions must be set");
        }

        [Theory]
        [InlineData("substring")]
        [InlineData("substringPreview")]
        public void Constructor_should_fail_when_substring_queryType_without_substringOptions(string queryType)
        {
            var exception = Record.Exception(() => new EncryptOptions(
                algorithm: EncryptionAlgorithm.String,
                keyId: Guid.NewGuid(),
                queryType: queryType,
                stringOptions: new StringOptions(true, true)));

            exception.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("SubstringOptions must be set");
        }

        [Fact]
        public void Constructor_should_fail_when_suffix_queryType_without_suffixOptions()
        {
            var exception = Record.Exception(() => new EncryptOptions(
                algorithm: EncryptionAlgorithm.String,
                keyId: Guid.NewGuid(),
                queryType: "suffix",
                stringOptions: new StringOptions(true, true)));

            exception.Should().BeOfType<ArgumentException>()
                .Which.Message.Should().Contain("SuffixOptions must be set");
        }

        [Theory]
        [InlineData(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic, "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic")]
        [InlineData(EncryptionAlgorithm.AEAD_AES_256_CBC_HMAC_SHA_512_Random, "AEAD_AES_256_CBC_HMAC_SHA_512-Random")]
        // these values are required to be supported due a CSHARP-3527 bug of how we worked with input algorithm values. So,
        // since we cannot remove them because of BC, we need to keep supporting them even after solving the underlying bug
        [InlineData("AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic", "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic")]
        [InlineData("AEAD_AES_256_CBC_HMAC_SHA_512_Random", "AEAD_AES_256_CBC_HMAC_SHA_512-Random")]
        // the below values match to the spec wording
        [InlineData("AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic", "AEAD_AES_256_CBC_HMAC_SHA_512-Deterministic")]
        [InlineData("AEAD_AES_256_CBC_HMAC_SHA_512-Random", "AEAD_AES_256_CBC_HMAC_SHA_512-Random")]
        // just a random string value
        [InlineData("TEST_random", "TEST_random")]
        // just a random value in enum form
        [InlineData((EncryptionAlgorithm)99, "99")]
        // indexed algorithm
        [InlineData(EncryptionAlgorithm.Indexed, "Indexed")]
        [InlineData("Indexed", "Indexed")]
        [InlineData(EncryptionAlgorithm.Unindexed, "Unindexed")]
        [InlineData("Unindexed", "Unindexed")]
        // range algorithm
        [InlineData(EncryptionAlgorithm.Range, "Range")]
        [InlineData("Range", "Range")]
        // String algorithm
        [InlineData(EncryptionAlgorithm.String, "String")]
        [InlineData("String", "String")]
        public void Constructor_should_support_different_algorithm_representations(object algorithm, string expectedAlgorithmRepresentation)
        {
            var alternateKeyName = "test";

            EncryptOptions subject;
            if (algorithm is EncryptionAlgorithm algorithmEnum)
            {
                subject = new EncryptOptions(algorithmEnum, alternateKeyName: alternateKeyName);
            }
            else
            {
                subject = new EncryptOptions(algorithm.ToString(), alternateKeyName: alternateKeyName);
            }

            subject.Algorithm.Should().Be(expectedAlgorithmRepresentation);
            subject.AlternateKeyName.Should().Be("test");
            subject.KeyId.Should().NotHaveValue();
        }

        [Fact]
        public void With_stringOptions_should_create_new_instance_with_updated_stringOptions()
        {
            var originalStringOptions = new StringOptions(true, true, prefixOptions: new PrefixOptions(10, 2));
            var newStringOptions = new StringOptions(false, false, substringOptions: new SubstringOptions(10, 8, 2));

            var subject = new EncryptOptions(algorithm: EncryptionAlgorithm.String, keyId: Guid.NewGuid(), stringOptions: originalStringOptions);

            var updated = subject.With(stringOptions: newStringOptions);

            updated.StringOptions.Should().BeSameAs(newStringOptions);
            updated.Algorithm.Should().Be(subject.Algorithm);
            updated.KeyId.Should().Be(subject.KeyId);
        }

        [Fact]
        public void StringOptions_should_render_all_query_type_options()
        {
            var subject = new StringOptions(
                caseSensitive: true,
                diacriticSensitive: false,
                prefixOptions: new PrefixOptions(10, 2),
                substringOptions: new SubstringOptions(20, 10, 2),
                suffixOptions: new SuffixOptions(8, 3));

            var result = subject.CreateDocument();

            result.Should().Be(BsonDocument.Parse(@"
                {
                    caseSensitive : true,
                    diacriticSensitive : false,
                    prefix : { strMaxQueryLength : 10, strMinQueryLength : 2 },
                    substring : { strMaxLength : 20, strMaxQueryLength : 10, strMinQueryLength : 2 },
                    suffix : { strMaxQueryLength : 8, strMinQueryLength : 3 }
                }"));
        }

        [Fact]
        public void StringOptions_should_omit_query_type_options_that_are_not_set()
        {
            var subject = new StringOptions(caseSensitive: false, diacriticSensitive: true);

            var result = subject.CreateDocument();

            result.Should().Be(BsonDocument.Parse("{ caseSensitive : false, diacriticSensitive : true }"));
        }

        [Fact]
        public void With_should_set_correct_values()
        {
            var originalAlgorithm = EncryptionAlgorithm.Indexed.ToString();
            var newAlgorithm = "newAlgorithm";
            var originalKeyId = Guid.Empty;
            var newKeyId = Guid.NewGuid();
            var originalAlternateKeyName = "test";
            var newAlternateKeyName = "new";
            long? originalContention = null;
            var newContention = 2;
            string originalQueryType = null;
            var newQueryType = "equality";

            var fle1WithKeyIdState = 0;
            var subject = CreateConfiguredSubject(state: fle1WithKeyIdState);
            AssertValues(subject, originalAlgorithm, expectedKeyId: originalKeyId);

            subject = subject.With(algorithm: newAlgorithm);
            AssertValues(subject, newAlgorithm, expectedKeyId: originalKeyId);

            subject = subject.With(keyId: newKeyId);
            AssertValues(subject, newAlgorithm, expectedKeyId: newKeyId);

            var fle1WithAlternateKeyNameState = 1;
            subject = CreateConfiguredSubject(state: fle1WithAlternateKeyNameState);
            AssertValues(subject, originalAlgorithm, expectedAlternateKeyName: originalAlternateKeyName);

            subject = subject.With(alternateKeyName: newAlternateKeyName);
            AssertValues(subject, originalAlgorithm, expectedAlternateKeyName: newAlternateKeyName);

            var fle2State = 2;
            subject = CreateConfiguredSubject(state: fle2State);
            subject = subject.With(contentionFactor: newContention);
            AssertValues(subject, expectedAlgorithm: originalAlgorithm, expectedKeyId: originalKeyId, expectedContentionFactor: newContention);

            newAlgorithm = EncryptionAlgorithm.Indexed.ToString();
            subject = CreateConfiguredSubject(state: fle2State);
            subject = subject.With(queryType: newQueryType);
            AssertValues(subject, expectedAlgorithm: newAlgorithm, expectedKeyId: originalKeyId, expectedQueryType: newQueryType);

            newQueryType = "range";
            newAlgorithm = EncryptionAlgorithm.Range.ToString();
            subject = CreateConfiguredSubject(state: fle2State);
            subject = subject.With(algorithm: EncryptionAlgorithm.Range.ToString(), queryType: newQueryType);
            AssertValues(subject, expectedAlgorithm: newAlgorithm, expectedKeyId: originalKeyId, expectedQueryType: newQueryType);

            static void AssertValues(EncryptOptions subject, string expectedAlgorithm, Guid? expectedKeyId = null, string expectedAlternateKeyName = null, string expectedQueryType = null, long? expectedContentionFactor = null)
            {
                subject.Algorithm.Should().Be(expectedAlgorithm);
                subject.KeyId.Should().Be(expectedKeyId);
                subject.AlternateKeyName.Should().Be(expectedAlternateKeyName);
                subject.QueryType.Should().Be(expectedQueryType);
                subject.ContentionFactor.Should().Be(expectedContentionFactor);
            }

            EncryptOptions CreateConfiguredSubject(int state)
            {
                switch (state)
                {
                    case 0: return new EncryptOptions(algorithm: originalAlgorithm, keyId: originalKeyId);
                    case 1: return new EncryptOptions(algorithm: originalAlgorithm, alternateKeyName: originalAlternateKeyName);
                    case 2: return new EncryptOptions(algorithm: originalAlgorithm, keyId: originalKeyId, contentionFactor: originalContention, queryType: originalQueryType);
                    default: throw new Exception($"Unexpected state: {state}.");
                }
            }
        }
    }
}
