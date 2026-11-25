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

public class AwsKmsProviderOptionsTests
{
    [Fact]
    public void Properties_should_default_to_null()
    {
        var subject = new AwsKmsProviderOptions();

        subject.AccessKeyId.Should().BeNull();
        subject.SecretAccessKey.Should().BeNull();
        subject.SessionToken.Should().BeNull();
        subject.Tls.Should().BeNull();
    }

    [Fact]
    public void ToKmsProviderOptions_should_return_an_empty_dictionary_for_on_demand_credentials()
    {
        var subject = new AwsKmsProviderOptions();

        subject.ToKmsProviderOptions(isNamed: false).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void ToKmsProviderOptions_should_treat_null_and_empty_credentials_as_on_demand(string accessKeyId, string secretAccessKey)
    {
        var subject = new AwsKmsProviderOptions { AccessKeyId = accessKeyId, SecretAccessKey = secretAccessKey };

        subject.ToKmsProviderOptions(isNamed: false).Should().BeEmpty();
    }

    [Fact]
    public void ToKmsProviderOptions_should_map_the_credentials()
    {
        var subject = new AwsKmsProviderOptions { AccessKeyId = "id", SecretAccessKey = "secret" };

        subject.ToKmsProviderOptions(isNamed: false).Should()
            .Equal(new Dictionary<string, object> { { "accessKeyId", "id" }, { "secretAccessKey", "secret" } });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToKmsProviderOptions_should_map_the_session_token(bool isNamed)
    {
        var subject = new AwsKmsProviderOptions { AccessKeyId = "id", SecretAccessKey = "secret", SessionToken = "token" };

        subject.ToKmsProviderOptions(isNamed).Should().Equal(new Dictionary<string, object>
        {
            { "accessKeyId", "id" }, { "secretAccessKey", "secret" }, { "sessionToken", "token" }
        });
    }

    [Theory]
    [InlineData("id", null)]
    [InlineData(null, "secret")]
    public void ToKmsProviderOptions_should_throw_when_the_credentials_are_not_set_together(string accessKeyId, string secretAccessKey)
    {
        var subject = new AwsKmsProviderOptions { AccessKeyId = accessKeyId, SecretAccessKey = secretAccessKey };

        var exception = Record.Exception(() => subject.ToKmsProviderOptions(isNamed: false));

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be("options");
    }

    [Fact]
    public void ToKmsProviderOptions_should_throw_when_the_session_token_is_set_without_credentials()
    {
        var subject = new AwsKmsProviderOptions { SessionToken = "token" };

        var exception = Record.Exception(() => subject.ToKmsProviderOptions(isNamed: false));

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be("options");
    }

    [Fact]
    public void ToKmsProviderOptions_should_throw_for_on_demand_credentials_of_a_named_provider()
    {
        var subject = new AwsKmsProviderOptions();

        var exception = Record.Exception(() => subject.ToKmsProviderOptions(isNamed: true));

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be("options");
    }
}
