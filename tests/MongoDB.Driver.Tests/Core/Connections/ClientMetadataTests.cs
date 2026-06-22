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
using MongoDB.Driver.Core.Connections;
using Xunit;

namespace MongoDB.Driver.Tests.Core.Connections;

public class ClientMetadataTests
{
    public ClientMetadataTests()
    {
        ClientDocumentHelper.Initialize();
    }

    [Fact]
    public void GetClientDocument_should_return_cached_instance_until_appended()
    {
        var subject = new ClientMetadata(applicationName: null, libraryInfo: null);

        var first = subject.GetClientDocument();
        subject.GetClientDocument().Should().BeSameAs(first);

        subject.Append(new LibraryInfo("lib", "1.0"));

        subject.GetClientDocument().Should().NotBeSameAs(first);
    }

    [Fact]
    public void Append_should_accumulate_library_infos_in_order()
    {
        var subject = new ClientMetadata(applicationName: null, new LibraryInfo("lib", "1.0"));

        subject.Append(new LibraryInfo("framework", "2.0"));

        var driverName = subject.GetClientDocument()["driver"]["name"].AsString;
        driverName.Should().EndWith("|lib|framework");
    }

    [Fact]
    public void Append_should_skip_empty_version_subfield()
    {
        var subject = new ClientMetadata(applicationName: null, libraryInfo: null);

        subject.Append(new LibraryInfo("lib1", version: null));
        subject.Append(new LibraryInfo("lib2", "2.0"));

        var driverVersion = subject.GetClientDocument()["driver"]["version"].AsString;
        driverVersion.Should().EndWith("|2.0");
        driverVersion.Split('|').Should().HaveCount(2); // base version + the single non-empty appended version
    }

    [Fact]
    public void Append_should_be_no_op_for_identical_library_info()
    {
        var subject = new ClientMetadata(applicationName: null, new LibraryInfo("lib", "1.0"));
        var before = subject.GetClientDocument();

        subject.Append(new LibraryInfo("lib", "1.0"));

        subject.GetClientDocument().Should().Be(before);
    }

    [Fact]
    public void Append_should_append_distinct_version_of_same_library()
    {
        var subject = new ClientMetadata(applicationName: null, new LibraryInfo("lib", "1.0"));

        subject.Append(new LibraryInfo("lib", "2.0"));

        var driver = subject.GetClientDocument()["driver"];
        driver["name"].AsString.Should().EndWith("|lib|lib");
        driver["version"].AsString.Should().EndWith("|1.0|2.0");
    }

    [Fact]
    public void Append_should_treat_empty_string_as_unset_for_dedup()
    {
        var subject = new ClientMetadata(applicationName: null, libraryInfo: null);
        subject.Append(new LibraryInfo("library", version: null, platform: "Library Platform"));
        var before = subject.GetClientDocument();

        subject.Append(new LibraryInfo("library", version: "", platform: "Library Platform")); // "" == unset, so identical => no-op

        subject.GetClientDocument().Should().Be(before);
    }

    [Fact]
    public void Append_should_throw_when_library_info_is_null()
    {
        var subject = new ClientMetadata(applicationName: null, libraryInfo: null);

        var exception = Record.Exception(() => subject.Append(null));

        exception.Should().BeOfType<ArgumentNullException>();
    }
}
