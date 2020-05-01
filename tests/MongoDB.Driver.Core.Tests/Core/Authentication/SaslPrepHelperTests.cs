/* Copyright 2018–present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using System;
using Xunit;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// SaslPrep unit tests.
    /// </summary>
    public class SaslPrepHelperTests
    {
        // Currently, we only support SaslPrep in .NET Framework due to a lack of a string normalization function in
        // .NET Standard
#if NET452
        [Fact]
        public void SaslPrepQuery_accepts_undefined_codepoint()
        {     
            var strWithUnassignedCodepoint = $"abc{char.ConvertFromUtf32(_unassignedCodePoint.Value)}";
            
            SaslPrepHelper.SaslPrepQuery(strWithUnassignedCodepoint).Should().Be(strWithUnassignedCodepoint);
        }

        [Fact]
        public void SaslPrepStored_accepts_RandALCat_Characters_in_first_and_last_position()
        {
            SaslPrepHelper.SaslPrepStored("\u0627\u0031\u0627").Should().Be("\u0627\u0031\u0627");
        }

        [Theory]
        [ParameterAttributeData]
        [InlineData("A B", "A\u00A0B")]
        [InlineData("A B", "A\u1680B")]
        public void SaslPrepStored_maps_space_equivalents_to_space(string expected, string input)
        {
            SaslPrepHelper.SaslPrepStored(input).Should().Be(expected);
        }

        [Theory]
        [ParameterAttributeData]
        [InlineData("IX", "\u2168")] // "IX", Roman numeral nine
        [InlineData("IV", "\u2163")] // "IV", Roman numeral four
        public void SaslPrepStored_returns_expected_output_when_passed_nonNormalized_strings(
            string expected,
            string nonNormalizedStr)
        {
            SaslPrepHelper.SaslPrepStored(nonNormalizedStr).Should().Be(expected);
        }

        [Theory]
        [ParameterAttributeData]
        [InlineData("IX", "I\u00ADX")] // "IX", "I-X"
        [InlineData("IV", "I\u00ADV")] // "IV", "I-V"
        public void SaslPrepStored_returns_expected_output_when_passed_partially_SaslPrepped_strings(
            string expected,
            string partiallyPreppedStr)
        {
            SaslPrepHelper.SaslPrepStored(partiallyPreppedStr).Should().Be(expected);
        }

        [Theory]
        [ParameterAttributeData]
        [InlineData("IX", "I\u00ADX")]
        [InlineData("user", "user")]
        [InlineData("user=", "user=")]
        [InlineData("USER", "USER")]
        [InlineData("a", "\u00AA")]
        [InlineData("IX", "\u2168")]
        public void SaslPrepStored_returns_expected_output_when_passed_Rfc4013_examples(string expected, string input)
        {
            SaslPrepHelper.SaslPrepStored(input).Should().Be(expected);
        }

        [Theory]
        [ParameterAttributeData]
        [InlineData("Prohibited character at position 0", "\u0007")]
        [InlineData("First character is RandALCat, but last character is not", "\u0627\u0031")]
        public void SaslPrep_throws_argument_exception_when_passed_Rfc4013_examples(string expectedError, string input)
        {
            var exception = Record.Exception(()=>SaslPrepHelper.SaslPrepStored(input));
            
            exception.Should().BeOfType<ArgumentException>().Subject.Message.Should().Be(expectedError);
        }

        [Fact]
        public void SaslPrepStored_throws_argument_exception_with_RandALCat_and_LCat_characters()
        {
            var exception = Record.Exception(() => SaslPrepHelper.SaslPrepStored("\u0627\u0041\u0627"));
            
            exception.Should().BeOfType<ArgumentException>();
            exception.Message.Should().Be("Contains both RandALCat characters and LCat characters");
        }
        
        [Fact]
        public void SaslPrepStored_throws_exception_when_passed_an_undefined_codepoint()
        { 
            var strWithUnassignedCodepoint = $"abc{char.ConvertFromUtf32(_unassignedCodePoint.Value)}";
            
            var exception = Record.Exception(()=>SaslPrepHelper.SaslPrepStored(strWithUnassignedCodepoint));
            
            exception.Should().BeOfType<ArgumentException>();
            exception.Message.Should().Be("Character at position 3 is unassigned");
        }
#endif

        private static readonly Lazy<int> _unassignedCodePoint = new Lazy<int>(FindUnassignedCodePoint);

        private static int FindUnassignedCodePoint()
        {
            for (var i = SaslPrepHelperReflector.MaxCodepoint; SaslPrepHelperReflector.MinCodepoint <= i; --i)
            {
                if (!SaslPrepHelperReflector.IsDefined(i) && !SaslPrepHelperReflector.Prohibited(i))
                {
                    return i;
                };
            }
            throw new Exception("Unable to find unassigned codepoint.");
        }
    }

    public static class SaslPrepHelperReflector
    {
        public static int MaxCodepoint =>
            (int)Reflector.GetStaticFieldValue(typeof(SaslPrepHelper), nameof(MaxCodepoint));

        public static int MinCodepoint =>
            (int)Reflector.GetStaticFieldValue(typeof(SaslPrepHelper), nameof(MinCodepoint));

        public static bool IsDefined(int codepoint) =>
            (bool)Reflector.InvokeStatic(typeof(SaslPrepHelper), nameof(IsDefined), codepoint);

        public static bool Prohibited(int codepoint) =>
            (bool)Reflector.InvokeStatic(typeof(SaslPrepHelper), nameof(Prohibited), codepoint);
    }
}
