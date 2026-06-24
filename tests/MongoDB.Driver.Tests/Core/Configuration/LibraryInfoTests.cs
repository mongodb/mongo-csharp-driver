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
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests.Core.Configuration;

public class LibraryInfoTests
{
    [Fact]
    public void constructor_should_initialize_properties()
    {
        var subject = new LibraryInfo("name", "version", "platform");

        subject.Name.Should().Be("name");
        subject.Version.Should().Be("version");
        subject.Platform.Should().Be("platform");
    }

    [Fact]
    public void constructor_without_platform_should_leave_platform_null()
    {
        var subject = new LibraryInfo("name", "version");

        subject.Platform.Should().BeNull();
    }

    [Theory]
    [InlineData("name", "version", "platform", true)]
    [InlineData("other", "version", "platform", false)]
    [InlineData("name", "other", "platform", false)]
    [InlineData("name", "version", "other", false)]
    [InlineData("name", "version", null, false)]
    public void Equals_should_compare_all_fields(string name, string version, string platform, bool expectedEqual)
    {
        var subject = new LibraryInfo("name", "version", "platform");
        var other = new LibraryInfo(name, version, platform);

        subject.Equals(other).Should().Be(expectedEqual);
    }

    [Theory]
    [InlineData("lib|x", "1.0", null, "name")]
    [InlineData("lib", "1.0|x", null, "version")]
    [InlineData("lib", "1.0", "platform|x", "platform")]
    public void constructor_should_throw_when_value_contains_separator(string name, string version, string platform, string expectedParamName)
    {
        var exception = Record.Exception(() => new LibraryInfo(name, version, platform));

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be(expectedParamName);
    }

    [Fact]
    public void GetHashCode_should_be_equal_for_equal_instances()
    {
        var subject = new LibraryInfo("name", "version", "platform");
        var equal = new LibraryInfo("name", "version", "platform");

        subject.GetHashCode().Should().Be(equal.GetHashCode());
    }
}
