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
using MongoDB.Driver.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class ClusterBuilderTests
{
    [Fact]
    public void SrvAllowedHostsSuffix_should_default_to_null()
    {
        var subject = new ClusterBuilder();

        subject.SrvAllowedHostsSuffix.Should().BeNull();
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData(".example.com")]
    [InlineData("sub.example.com")]
    public void SrvAllowedHostsSuffix_should_accept_valid_values(string value)
    {
        var subject = new ClusterBuilder();

        subject.SrvAllowedHostsSuffix = value;

        subject.SrvAllowedHostsSuffix.Should().Be(value);
    }

    [Fact]
    public void SrvAllowedHostsSuffix_should_accept_null_after_being_set()
    {
        var subject = new ClusterBuilder { SrvAllowedHostsSuffix = "example.com" };

        subject.SrvAllowedHostsSuffix = null;

        subject.SrvAllowedHostsSuffix.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("notallowedsingle")]
    public void SrvAllowedHostsSuffix_should_throw_for_invalid_values(string value)
    {
        var subject = new ClusterBuilder { SrvAllowedHostsSuffix = "example.com" };

        var exception = Record.Exception(() => subject.SrvAllowedHostsSuffix = value);

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("SrvAllowedHostsSuffix");
        subject.SrvAllowedHostsSuffix.Should().Be("example.com");
    }
}
