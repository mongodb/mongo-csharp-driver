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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers.Core;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Authentication.Oidc;

public class FileOidcCallbackTests
{
    [Fact]
    public void FileOidcCallback_ctor_throws_on_empty_fileSystemProvider()
    {
        var exception = Record.Exception(() => new FileOidcCallback(null, "filePath"));
        exception.Should().BeOfType<ArgumentNullException>().Subject.ParamName.Should().Be("fileSystemProvider");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FileOidcCallback_ctor_throws_on_empty_filePath(string filePath)
    {
        var exception = Record.Exception(() => new FileOidcCallback(Mock.Of<IFileSystemProvider>(), filePath));
        exception.Should().BeAssignableTo<ArgumentException>().Subject.ParamName.Should().Be("filePath");
    }

    [Theory]
    [ParameterAttributeData]
    public async Task GetOidcAccessToken_calls_fileSystemProvider([Values(true, false)]bool async)
    {
        var filePath = "some-file-path";
        var fileContent = "some-content";
        var fileSystemProviderMock = new Mock<IFileSystemProvider>();
        var fileMock = new Mock<IFile>();
        fileMock.Setup(f => f.ReadAllText(filePath)).Returns(fileContent);
        fileMock.Setup(f => f.ReadAllTextAsync(filePath)).Returns(Task.FromResult(fileContent));
        fileSystemProviderMock.Setup(f => f.File).Returns(fileMock.Object);
        var oidcParameters = new OidcCallbackParameters(1, "userName");

        var oidcCallback = new FileOidcCallback(fileSystemProviderMock.Object, filePath);
        var result = async ?
            await oidcCallback.GetOidcAccessTokenAsync(oidcParameters, default):
            oidcCallback.GetOidcAccessToken(oidcParameters, default);

        result.AccessToken.Should().Be(fileContent);
        if (async)
        {
            fileMock.Verify(f => f.ReadAllTextAsync(filePath), Times.Once);
        }
        else
        {
            fileMock.Verify(f => f.ReadAllText(filePath), Times.Once);
        }
    }

    [Fact]
    public void CreateFromEnvironmentVariable_throws_on_null_environmentVariableProvider()
    {
        var exception = Record.Exception(() =>
        {
            FileOidcCallback.CreateFromEnvironmentVariable(null, Mock.Of<IFileSystemProvider>(), ["env"], "defaultPath");
        });

        exception.Should().BeOfType<ArgumentNullException>().Subject.ParamName.Should().Be("environmentVariableProvider");
    }

    [Fact]
    public void CreateFromEnvironmentVariable_throws_on_null_fileSystemProvider()
    {
        var exception = Record.Exception(() =>
        {
            FileOidcCallback.CreateFromEnvironmentVariable(Mock.Of<IEnvironmentVariableProvider>(), null, ["env"], "defaultPath");
        });

        exception.Should().BeOfType<ArgumentNullException>().Subject.ParamName.Should().Be("fileSystemProvider");
    }

    [Fact]
    public void CreateFromEnvironmentVariable_throws_on_null_environmentVariableNames()
    {
        var exception = Record.Exception(() =>
        {
            FileOidcCallback.CreateFromEnvironmentVariable(Mock.Of<IEnvironmentVariableProvider>(), Mock.Of<IFileSystemProvider>(), null, "defaultPath");
        });

        exception.Should().BeOfType<ArgumentNullException>().Subject.ParamName.Should().Be("environmentVariableNames");
    }

    [Fact]
    public void CreateFromEnvironmentVariable_throws_on_empty_environmentVariableNames()
    {
        var exception = Record.Exception(() =>
        {
            FileOidcCallback.CreateFromEnvironmentVariable(Mock.Of<IEnvironmentVariableProvider>(), Mock.Of<IFileSystemProvider>(), [], "defaultPath");
        });

        exception.Should().BeOfType<ArgumentException>().Subject.ParamName.Should().Be("environmentVariableNames");
    }

    [Theory]
    [MemberData(nameof(CreateFromEnvironmentVariable_ValidTestCases))]
    public void CreateFromEnvironmentVariable_should_resolve_filePath(string[] env, string[] variablesToCheck, string defaultPath, string expectedFilePath)
    {
        var environmentVariableProviderMock = EnvironmentVariableProviderMock.Create(env);

        var fileOidcCallback = FileOidcCallback.CreateFromEnvironmentVariable(environmentVariableProviderMock.Object, Mock.Of<IFileSystemProvider>(), variablesToCheck, defaultPath);

        fileOidcCallback.FilePath.Should().Be(expectedFilePath);
    }

    [Theory]
    [MemberData(nameof(CreateFromEnvironmentVariable_InvalidTestCases))]
    public void CreateFromEnvironmentVariable_should_throw_on_unresolvable_filePath(string[] env, string[] variablesToCheck)
    {
        var environmentVariableProviderMock = EnvironmentVariableProviderMock.Create(env);

        var exception = Record.Exception(() => FileOidcCallback.CreateFromEnvironmentVariable(environmentVariableProviderMock.Object, Mock.Of<IFileSystemProvider>(), variablesToCheck));

        exception.Should().BeAssignableTo<ArgumentException>().Subject.ParamName.Should().Be("filePath");
    }

    public static IEnumerable<object[]> CreateFromEnvironmentVariable_ValidTestCases =
    [
        [
            null,
            new[] { "env1", "env2" },
            "defaultPath",
            "defaultPath"
        ],
        [
            new[] { "env1=path1", "env2=path2" },
            new[] { "env3", "env4" },
            "defaultPath",
            "defaultPath"
        ],
        [
            new[] { "env1=path1", "env2=path2" },
            new[] { "env3", "env1" },
            "defaultPath",
            "path1"
        ]
    ];

    public static IEnumerable<object[]> CreateFromEnvironmentVariable_InvalidTestCases =
    [
        [
            null,
            new[] { "env1", "env2" }
        ],
        [
            new[] { "env1=path1", "env2=path2" },
            new[] { "env3", "env4" }
        ]
    ];
}
