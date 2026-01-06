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
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Core.Misc.Tests;

public class DefaultRandomTests
{
    [Theory]
    [InlineData(1, "a")]
    [InlineData(1, "abcdefghijklmnopqrstuvwxyz")]
    [InlineData(10, "a")]
    [InlineData(10, "abcdefghijklmnopqrstuvwxyz")]
    public void GenerateString_returns_expected_result(int length, string legalCharacters)
    {
        var result = DefaultRandom.Instance.GenerateString(length, legalCharacters);

        result.Length.Should().Be(length);
        result.Should().Match(value => value.All(c => legalCharacters.Contains(c)));
    }

    [Fact]
    public void GenerateString_returns_empty_string_for_zero_length()
    {
        var result = DefaultRandom.Instance.GenerateString(0, "abc");
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void GenerateString_throws_on_negative_length()
    {
        var ex = Record.Exception(() => DefaultRandom.Instance.GenerateString(-5, "abc"));
        ex.Should().BeOfType<ArgumentOutOfRangeException>().Subject
            .ParamName.Should().Be("length");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GenerateString_throws_on_empty_legalCharacters(string legalCharacters)
    {
        var ex = Record.Exception(() => DefaultRandom.Instance.GenerateString(5, legalCharacters));
        ex.Should().BeAssignableTo<ArgumentException>().Subject
            .ParamName.Should().Be("legalCharacters");
    }


}

