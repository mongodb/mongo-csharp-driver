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

public class KmipKmsProviderOptionsTests
{
    [Fact]
    public void Properties_should_default_to_null()
    {
        var subject = new KmipKmsProviderOptions();

        subject.Endpoint.Should().BeNull();
        subject.Tls.Should().BeNull();
    }

    [Fact]
    public void ToKmsProviderOptions_should_map_the_endpoint()
    {
        var subject = new KmipKmsProviderOptions { Endpoint = "kmip.example.com:5696" };

        subject.ToKmsProviderOptions().Should().Equal(new Dictionary<string, object> { { "endpoint", "kmip.example.com:5696" } });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToKmsProviderOptions_should_throw_when_the_endpoint_is_not_set(string endpoint)
    {
        var subject = new KmipKmsProviderOptions { Endpoint = endpoint };

        var exception = Record.Exception(() => subject.ToKmsProviderOptions());

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be("options");
    }
}
