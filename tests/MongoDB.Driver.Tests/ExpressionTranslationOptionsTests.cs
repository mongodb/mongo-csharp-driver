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
using Shouldly;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ExpressionTranslationOptionsTests
    {
        [Fact]
        public void Constructor_should_return_expected_result()
        {
            var subject = new ExpressionTranslationOptions();

            subject.CompatibilityLevel.ShouldBe(null);
            subject.EnableClientSideProjections.ShouldBe(null);
        }

        [Fact]
        public void CompatibilityLevel_should_return_expected_result()
        {
            var subject = new ExpressionTranslationOptions();
            subject.CompatibilityLevel.ShouldBe(null);

            subject.CompatibilityLevel = ServerVersion.Server26;

            subject.CompatibilityLevel.ShouldBe(ServerVersion.Server26);
        }

        [Fact]
        public void EnableClientSideProjections_should_return_expected_result()
        {
            var subject = new ExpressionTranslationOptions();
            subject.EnableClientSideProjections.ShouldBe(null);

            subject.EnableClientSideProjections = false;

            subject.EnableClientSideProjections.ShouldBe(false);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ExpressionTranslationOptions();

            var result = x.Equals(null);

            result.ShouldBe(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ExpressionTranslationOptions();
            var y = new object();

            var result = x.Equals(y);

            result.ShouldBe(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ExpressionTranslationOptions();

            var result = x.Equals(x);

            result.ShouldBe(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ExpressionTranslationOptions();
            var y = new ExpressionTranslationOptions();

            var result = x.Equals(y);

            result.ShouldBe(true);
        }

        [Theory]
        [InlineData("CompatibilityLevel")]
        [InlineData("EnableClientSideProjections")]
        public void Equals_with_not_equal_field_should_return_false(string notEqualFieldName)
        {
            var x = new ExpressionTranslationOptions();
            var y = notEqualFieldName switch
            {
                "CompatibilityLevel" => new ExpressionTranslationOptions { CompatibilityLevel = ServerVersion.Server40 },
                "EnableClientSideProjections" => new ExpressionTranslationOptions { EnableClientSideProjections = false },
                _ => throw new Exception()
            };

            var result = x.Equals(y);

            result.ShouldBe(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ExpressionTranslationOptions();

            var result = x.GetHashCode();

            result.ShouldBe(0);
        }

        [Theory]
        [InlineData(null, null, "{ CompatibilityLevel = null, EnableClientSideProjections = null }")]
        [InlineData(null, true, "{ CompatibilityLevel = null, EnableClientSideProjections = True }")]
        [InlineData(ServerVersion.Server40, null, "{ CompatibilityLevel = Server40, EnableClientSideProjections = null }")]
        [InlineData(ServerVersion.Server40, true, "{ CompatibilityLevel = Server40, EnableClientSideProjections = True }")]
        public void ToString_should_return_expected_result(ServerVersion? compatibilityLevel, bool? enableClientSideProjections, string expectedResult)
        {
            var subject = new ExpressionTranslationOptions { CompatibilityLevel = compatibilityLevel, EnableClientSideProjections = enableClientSideProjections };

            var result = subject.ToString();

            result.ShouldBe(expectedResult);
        }
    }
}
