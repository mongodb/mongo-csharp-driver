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
using MongoDB.Driver.Core.Configuration;
using Xunit;

namespace MongoDB.Driver.Tests.Configuration;

public class ClientMetadataBuilderTests
{
    [Fact]
    public void Properties_should_default_to_null()
    {
        var subject = new ClientMetadataBuilder();

        subject.ApplicationName.Should().BeNull();
        subject.LibraryInfo.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("app")]
    public void ApplicationName_should_accept_valid_values(string value)
    {
        var subject = new ClientMetadataBuilder();

        subject.ApplicationName = value;

        subject.ApplicationName.Should().Be(value);
    }

    [Fact]
    public void ApplicationName_should_accept_128_bytes()
    {
        var subject = new ClientMetadataBuilder();
        var value = new string('a', 128);

        subject.ApplicationName = value;

        subject.ApplicationName.Should().Be(value);
    }

    [Fact]
    public void ApplicationName_should_throw_when_longer_than_128_bytes()
    {
        var subject = new ClientMetadataBuilder();

        var exception = Record.Exception(() => subject.ApplicationName = new string('a', 129));

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("ApplicationName");
    }

    [Fact]
    public void ApplicationName_should_measure_the_length_in_utf8_bytes()
    {
        var subject = new ClientMetadataBuilder();

        // 65 characters, but 130 bytes once encoded
        var exception = Record.Exception(() => subject.ApplicationName = new string('é', 65));

        exception.Should().BeOfType<ArgumentException>()
            .Subject.ParamName.Should().Be("ApplicationName");
    }

    [Fact]
    public void ApplicationName_should_keep_the_previous_value_when_the_new_one_is_invalid()
    {
        var subject = new ClientMetadataBuilder { ApplicationName = "app" };

        Record.Exception(() => subject.ApplicationName = new string('a', 129));

        subject.ApplicationName.Should().Be("app");
    }

    [Fact]
    public void LibraryInfo_should_be_settable()
    {
        var libraryInfo = new LibraryInfo("lib", "1.0");
        var subject = new ClientMetadataBuilder();

        subject.LibraryInfo = libraryInfo;

        subject.LibraryInfo.Should().BeSameAs(libraryInfo);
    }

    [Fact]
    public void ApplyConnectionString_should_apply_the_application_name()
    {
        var subject = new ClientMetadataBuilder();

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1/?appName=app"));

        subject.ApplicationName.Should().Be("app");
    }

    [Fact]
    public void ApplyConnectionString_should_keep_the_application_name_when_it_is_not_specified()
    {
        var subject = new ClientMetadataBuilder { ApplicationName = "app" };

        subject.ApplyConnectionString(new ConnectionString("mongodb://host1"));

        subject.ApplicationName.Should().Be("app");
    }
}
