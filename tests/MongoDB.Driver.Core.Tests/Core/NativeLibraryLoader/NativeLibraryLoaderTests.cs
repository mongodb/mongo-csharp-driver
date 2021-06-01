/* Copyright 2021-present MongoDB Inc.
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
using System.IO;
using FluentAssertions;
using MongoDB.Driver.Core.NativeLibraryLoader;
using MongoDB.Driver.TestHelpers;
using MongoDB.Shared;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.NativeLibraryLoader
{
    public class NativeLibraryLoaderTests
    {
        [Fact]
        public void GetLibraryBasePath_should_get_correct_paths_for_assembly_based_path()
        {
            var subject = new TestRelativeLibraryLocator(mockedAssemblyUri: null);

            var result = subject.GetLibraryBasePath();

            // Ideally the root folder for expectedResult should be mongo-csharp-driver,
            // but since it's not mocked logic it limits us where we can run our tests from. Avoid it by
            // making a test assertation less straight
            var expectedResult = GetCommonTestAssemblyFolderEnding();
            result.Should().EndWith(expectedResult);
        }

        [Theory]
        [InlineData("mongo-csharp-driver", "mongo-csharp-driver")]
        [InlineData("mongo csharp driver", "mongo csharp driver")]
        [InlineData("&mongo$csharp@driver%", "&mongo$csharp@driver%")]
        public void GetLibraryBasePath_should_get_correct_paths_with_mocking(string rootTestFolder, string expectedRootTestFolder)
        {
            var assemblyCodeBase = Path.Combine(
                RequirePlatform.GetCurrentOperatingSystem() == SupportedOperatingSystem.Windows ? "C:/" : @"\\data",
                rootTestFolder,
                GetCommonTestAssemblyFolderEnding(),
                "MongoDB.Driver.Core.dll");
            var testAssemblyCodeBaseUri = new Uri(assemblyCodeBase).ToString();
            var subject = new TestRelativeLibraryLocator(mockedAssemblyUri: testAssemblyCodeBaseUri);

            var result = subject.GetLibraryBasePath();

            var expectedResult = Path.Combine(expectedRootTestFolder, GetCommonTestAssemblyFolderEnding());
            result.Should().EndWith(expectedResult);
        }

        // private methods
        private string GetCommonTestAssemblyFolderEnding() =>
            Path.Combine(
                "tests",
                "MongoDB.Driver.Core.Tests",
                "bin",
                GetConfigurationName(),
                GetTargetFrameworkMonikerName());

        private string GetConfigurationName() =>
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        private string GetTargetFrameworkMonikerName() =>
#if NETCOREAPP1_1
            "netcoreapp1.1";
#elif NETCOREAPP2_1
            "netcoreapp2.1";
#elif NETCOREAPP3_0
            "netcoreapp3.0";
#elif NET452
            "net452";
#endif

        // nested types
        private class TestRelativeLibraryLocator : RelativeLibraryLocatorBase
        {
            private readonly string _mockedAssemblyUri;

            public TestRelativeLibraryLocator(string mockedAssemblyUri)
            {
                _mockedAssemblyUri = mockedAssemblyUri; // can be null
            }

            public override string GetBaseAssemblyUri() => _mockedAssemblyUri ?? base.GetBaseAssemblyUri();

            // not required for these tests yet
            public override string GetLibraryRelativePath(OperatingSystemPlatform currentPlatform) => throw new NotImplementedException();
        }
    }
}
