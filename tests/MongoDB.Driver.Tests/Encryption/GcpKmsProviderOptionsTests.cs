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
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Driver.Encryption;
using Xunit;

namespace MongoDB.Driver.Tests.Encryption;

public class GcpKmsProviderOptionsTests
{
    [Fact]
    public void Properties_should_default_to_null()
    {
        var subject = new GcpKmsProviderOptions();

        subject.Email.Should().BeNull();
        subject.PrivateKey.Should().BeNull();
        subject.Endpoint.Should().BeNull();
        subject.Tls.Should().BeNull();
    }

    [Fact]
    public void ToKmsProviderOptions_should_return_an_empty_dictionary_for_on_demand_credentials()
    {
        var subject = new GcpKmsProviderOptions();

        subject.ToKmsProviderOptions(isNamed: false).Should().BeEmpty();
    }

    [Fact]
    public void ToKmsProviderOptions_should_map_the_credentials()
    {
        var subject = new GcpKmsProviderOptions { Email = "user@example.com", PrivateKey = "key" };

        subject.ToKmsProviderOptions(isNamed: false).Should()
            .Equal(new Dictionary<string, object> { { "email", "user@example.com" }, { "privateKey", "key" } });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToKmsProviderOptions_should_map_the_endpoint(bool isNamed)
    {
        var subject = new GcpKmsProviderOptions { Email = "user@example.com", PrivateKey = "key", Endpoint = "oauth2.example.com" };

        subject.ToKmsProviderOptions(isNamed).Should().Equal(new Dictionary<string, object>
        {
            { "email", "user@example.com" }, { "privateKey", "key" }, { "endpoint", "oauth2.example.com" }
        });
    }

    [Theory]
    [InlineData("user@example.com", null)]
    [InlineData(null, "key")]
    public void ToKmsProviderOptions_should_throw_when_the_credentials_are_not_set_together(string email, string privateKey)
    {
        var subject = new GcpKmsProviderOptions { Email = email, PrivateKey = privateKey };

        var exception = Record.Exception(() => subject.ToKmsProviderOptions(isNamed: false));

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be("options");
    }

    [Fact]
    public void ToKmsProviderOptions_should_throw_when_the_endpoint_is_set_without_credentials()
    {
        var subject = new GcpKmsProviderOptions { Endpoint = "oauth2.example.com" };

        var exception = Record.Exception(() => subject.ToKmsProviderOptions(isNamed: false));

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be("options");
    }

    [Fact]
    public void ToKmsProviderOptions_should_throw_for_on_demand_credentials_of_a_named_provider()
    {
        var subject = new GcpKmsProviderOptions();

        var exception = Record.Exception(() => subject.ToKmsProviderOptions(isNamed: true));

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be("options");
    }
}
