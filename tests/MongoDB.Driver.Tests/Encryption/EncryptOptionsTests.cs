/* Copyright 2021-present MongoDB Inc.
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
            e.Message.Should().Be("ContentionFactor only applies for Indexed or RangePreview algorithm.");
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
            e.Message.Should().Be("QueryType only applies for Indexed or RangePreview algorithm.");
        }

        [Fact]
        public void Constructor_should_fail_when_rangeOptions_and_algorithm_is_not_rangePreview()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: "test", keyId: Guid.NewGuid(), rangeOptions: new RangeOptions(0)));
            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.Message.Should().Be("RangeOptions only applies for RangePreview algorithm.");
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
        // range preview algorithm
        [InlineData(EncryptionAlgorithm.RangePreview, "RangePreview")]
        [InlineData("RangePreview", "RangePreview")]
        public void Constructor_should_support_different_algorithm_representations(object algorithm, string expectedAlgorithmRepresentation)
        {
            var alternateKeyName = "test";

            EncryptOptions subject;
            if (algorithm is EncryptionAlgorithm enumedAlgorithm)
            {
                subject = new EncryptOptions(enumedAlgorithm, alternateKeyName: alternateKeyName);
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
            AssertValues(subject, newAlgorithm, expectedKeyId:  newKeyId);

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

            newQueryType = "rangePreview";
            newAlgorithm = EncryptionAlgorithm.RangePreview.ToString();
            subject = CreateConfiguredSubject(state: fle2State);
            subject = subject.With(algorithm: EncryptionAlgorithm.RangePreview.ToString(), queryType: newQueryType);
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
                    case 1:  return new EncryptOptions(algorithm: originalAlgorithm, alternateKeyName: originalAlternateKeyName);
                    case 2: return new EncryptOptions(algorithm: originalAlgorithm, keyId: originalKeyId, contentionFactor: originalContention, queryType: originalQueryType);
                    default: throw new Exception($"Unexpected state: {state}.");
                }
            }
        }
    }
}
