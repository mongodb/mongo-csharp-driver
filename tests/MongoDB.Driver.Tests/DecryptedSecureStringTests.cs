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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class DecryptedSecureStringTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var secureString = new SecureString();

            var result = new DecryptedSecureString(secureString);

            result._chars().Should().BeNull();
            result._charsHandle().IsAllocated.Should().BeFalse();
            result._charsIntPtr().Should().Be(IntPtr.Zero);
            result._disposed().Should().BeFalse();
            result._secureString().Should().BeSameAs(secureString);
            result._utf8Bytes().Should().BeNull();
            result._utf8BytesHandle().IsAllocated.Should().BeFalse();
        }

        [Fact]
        public void Dispose_should_set_the_disposed_flag()
        {
            var subject = CreateSubject();
            subject._disposed().Should().BeFalse();

            subject.Dispose();

            subject._disposed().Should().BeTrue();
        }

        [Fact]
        public void Dispose_can_be_called_more_than_once()
        {
            var subject = CreateSubject();

            subject.Dispose();
            subject.Dispose();
        }

        [Fact]
        public void Dispose_should_clear_chars()
        {
            var subject = CreateSubject();
            var chars = subject.GetChars();
            chars.All(c => c == 0).Should().BeFalse();

            subject.Dispose();

            chars.All(c => c == 0).Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_free_charsHandle()
        {
            var subject = CreateSubject();
            subject.GetChars();
            subject._charsHandle().IsAllocated.Should().BeTrue();

            subject.Dispose();

            subject._charsHandle().IsAllocated.Should().BeFalse();
        }

        [Fact]
        public void Dispose_should_zero_charsIntPtr()
        {
            var subject = CreateSubject();
            subject.GetChars();
            subject._charsIntPtr().Should().NotBe(IntPtr.Zero);

            subject.Dispose();

            subject._charsIntPtr().Should().Be(IntPtr.Zero);
        }

        [Fact]
        public void Dispose_should_clear_utf8Bytes()
        {
            var subject = CreateSubject();
            var utf8Bytes = subject.GetUtf8Bytes();
            utf8Bytes.All(b => b == 0).Should().BeFalse();

            subject.Dispose();

            utf8Bytes.All(b => b == 0).Should().BeTrue();
        }

        [Fact]
        public void Dispose_should_free_utf8BytesHandle()
        {
            var subject = CreateSubject();
            subject.GetUtf8Bytes();
            subject._utf8BytesHandle().IsAllocated.Should().BeTrue();

            subject.Dispose();

            subject._utf8BytesHandle().IsAllocated.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChars_should_return_expected_result(
            [Values("", "a", "ab", "abc")] string value)
        {
            var subject = CreateSubject(value);

            var result = subject.GetChars();

            result.Should().Equal(value);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetUtf8Bytes_should_return_expected_result(
            [Values("", "a", "ab", "abc")] string value)
        {
            var subject = CreateSubject(value);
            var expectedResult = Utf8Encodings.Strict.GetBytes(subject.GetChars());

            var result = subject.GetUtf8Bytes();

            result.Should().Equal(expectedResult);
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

        private DecryptedSecureString CreateSubject(string value = "abc")
        {
            var secureString = CreateSecureString(value);
            return new DecryptedSecureString(secureString);
        }
    }

    internal static class DecryptedSecureStringReflector
    {
        public static char[] _chars(this DecryptedSecureString obj)
        {
            var fieldInfo = typeof(DecryptedSecureString).GetField("_chars", BindingFlags.NonPublic | BindingFlags.Instance);
            return (char[])fieldInfo.GetValue(obj);
        }

        public static GCHandle _charsHandle(this DecryptedSecureString obj)
        {
            var fieldInfo = typeof(DecryptedSecureString).GetField("_charsHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            return (GCHandle)fieldInfo.GetValue(obj);
        }

        public static IntPtr _charsIntPtr(this DecryptedSecureString obj)
        {
            var fieldInfo = typeof(DecryptedSecureString).GetField("_charsIntPtr", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IntPtr)fieldInfo.GetValue(obj);
        }

        public static bool _disposed(this DecryptedSecureString obj)
        {
            var fieldInfo = typeof(DecryptedSecureString).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)fieldInfo.GetValue(obj);
        }

        public static SecureString _secureString(this DecryptedSecureString obj)
        {
            var fieldInfo = typeof(DecryptedSecureString).GetField("_secureString", BindingFlags.NonPublic | BindingFlags.Instance);
            return (SecureString)fieldInfo.GetValue(obj);
        }

        public static byte[] _utf8Bytes(this DecryptedSecureString obj)
        {
            var fieldInfo = typeof(DecryptedSecureString).GetField("_utf8Bytes", BindingFlags.NonPublic | BindingFlags.Instance);
            return (byte[])fieldInfo.GetValue(obj);
        }

        public static GCHandle _utf8BytesHandle(this DecryptedSecureString obj)
        {
            var fieldInfo = typeof(DecryptedSecureString).GetField("_utf8BytesHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            return (GCHandle)fieldInfo.GetValue(obj);
        }
    }
}
