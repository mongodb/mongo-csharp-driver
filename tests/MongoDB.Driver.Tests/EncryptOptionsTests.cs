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

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "FLE")]
    public class EncryptOptionsTests
    {
        [Fact]
        public void Constructor_should_fail_when_algorithm_is_null()
        {
            var exception = Record.Exception(() => new EncryptOptions(algorithm: null));
            exception.Should().BeOfType<ArgumentNullException>();
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
            var originalAlgorithm = "originalAlgorithm";
            var newAlgorithm = "newAlgorithm";
            var originalKeyId = Guid.Empty;
            var newKeyId = Guid.NewGuid();
            var originalAlternateKeyName = "test";
            var newAlternateKeyName = "new";

            var subject = CreateConfiguredSubject(withKeyId: true);
            AssertValues(subject, originalAlgorithm, originalKeyId, null);

            subject = subject.With(algorithm: newAlgorithm);
            AssertValues(subject, newAlgorithm, originalKeyId, null);

            subject = subject.With(keyId: newKeyId);
            AssertValues(subject, newAlgorithm, newKeyId, null);

            subject = CreateConfiguredSubject(withKeyId: false);
            AssertValues(subject, originalAlgorithm, null, originalAlternateKeyName);

            subject = subject.With(alternateKeyName: newAlternateKeyName);
            AssertValues(subject, originalAlgorithm, null, newAlternateKeyName);

            static void AssertValues(EncryptOptions subject, string algorithm, Guid? keyId, string alternateKeyName)
            {
                subject.Algorithm.Should().Be(algorithm);
                subject.KeyId.Should().Be(keyId);
                subject.AlternateKeyName.Should().Be(alternateKeyName);
            }

            EncryptOptions CreateConfiguredSubject(bool withKeyId)
            {
                if (withKeyId)
                {
                    return new EncryptOptions(algorithm: originalAlgorithm, keyId: originalKeyId);
                }
                else
                {
                    return new EncryptOptions(algorithm: originalAlgorithm, alternateKeyName: originalAlternateKeyName);
                }
            }
        }
    }
}
