/* Copyright 2018-present MongoDB Inc.
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
using System.Security;
using FluentAssertions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class PasswordEvidenceTests
    {
        [Theory]
        [ParameterAttributeData]
        public void constructor_with_SecureString_should_initialize_instance(
            [Values("", "a", "ab", "abc")] string value)
        {
            var securePassword = CreateSecureString(value);

            var result = new PasswordEvidence(securePassword);

            var decryptedPassword = new DecryptedSecureString(result.SecurePassword);
            decryptedPassword.GetChars().Should().Equal(value);
        }

        [Fact]
        public void constructor_with_SecureString_should_throw_when_password_is_null()
        {
            var password = (SecureString)null;

            var exception = Record.Exception(() => new PasswordEvidence(password));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("password");
        }

        [Fact]
        public void constructor_with_SecureString_should_make_a_read_only_copy_of_password()
        {
            var securePassword = CreateSecureString("abc");
            securePassword.IsReadOnly().Should().BeFalse();

            var result = new PasswordEvidence(securePassword);

            result.SecurePassword.Should().NotBeSameAs(securePassword);
            result.SecurePassword.IsReadOnly().Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_string_should_initialize_instance(
            [Values("", "a", "ab", "abc")] string value)
        {
            var result = new PasswordEvidence(value);

            var decryptedPassword = new DecryptedSecureString(result.SecurePassword);
            decryptedPassword.GetChars().Should().Equal(value);
        }

        [Fact]
        public void constructor_with_string_should_throw_when_password_is_null()
        {
            var password = (string)null;

            var exception = Record.Exception(() => new PasswordEvidence(password));

            var e = exception.Should().BeOfType<ArgumentNullException>().Subject;
            e.ParamName.Should().Be("password");
        }

        [Fact]
        public void constructor_with_string_should_create_a_read_only_securePassword()
        {
            var result = new PasswordEvidence("abc");

            result.SecurePassword.IsReadOnly().Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void SecurePassword_should_return_expected_result(
            [Values("", "a", "ab", "abc")] string value)
        {
            var subject = CreateSubject(value);

            var result = subject.SecurePassword;

            var decryptedPassword = new DecryptedSecureString(result);
            decryptedPassword.GetChars().Should().Equal(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_return_true_when_both_all_fields_are_equal(
            [Values("", "a", "ab", "abc")] string value)
        {
            var subject = CreateSubject(value);
            var other = CreateSubject(value);

            var result = subject.Equals(other);
            var hashCode1 = subject.GetHashCode();
            var hashCode2 = other.GetHashCode();

            result.Should().BeTrue();
            hashCode2.Should().Be(hashCode1);
        }

        [Theory]
        [InlineData("abc", "def")]
        public void Equals_should_return_false_when_any_field_is_not_equal(string x, string y)
        {
            var subject = CreateSubject(x);
            var other = CreateSubject(y);

            var result = subject.Equals(other);
            var hashCode1 = subject.GetHashCode();
            var hashCode2 = other.GetHashCode();

            result.Should().BeFalse();
            hashCode2.Should().NotBe(hashCode1);
        }

        [Fact]
        public void Equals_should_return_false_when_other_is_null()
        {
            var subject = CreateSubject();
            var other = (PasswordEvidence)null;

            var result = subject.Equals(other);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_when_other_is_of_a_different_type()
        {
            var subject = CreateSubject();
            var other = new object();

            var result = subject.Equals(other);

            result.Should().BeFalse();
        }

        // private methods
        private SecureString CreateSecureString(string value)
        {
            var result = new SecureString();
            foreach (var c in value)
            {
                result.AppendChar(c);
            }
            return result;
        }

        private PasswordEvidence CreateSubject(string value = "abc")
        {
            return new PasswordEvidence(value);
        }
    }
}
