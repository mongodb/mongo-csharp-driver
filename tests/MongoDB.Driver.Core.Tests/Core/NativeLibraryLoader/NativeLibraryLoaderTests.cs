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
        [Theory]
        [InlineData(null, "mongo-csharp-driver")] // the default assembly-based logic
        [InlineData("mongo-csharp-driver", "mongo-csharp-driver")]
        [InlineData("mongo csharp driver", "mongo csharp driver")]
        [InlineData("&mongo$csharp@driver%", "&mongo$csharp@driver%")]
        public void GetLibraryBasePath_should_get_correct_paths(string rootTestFolder, string expectedRootTestFolder)
        {
            string testAssemblyCodeBaseUri = null; // use a default one for null
            if (rootTestFolder != null)
            {
                var assemblyPath = Path.Combine(
                    RequirePlatform.GetCurrentOperatingSystem() == SupportedOperatingSystem.Windows ? "C:/" : @"\\data",
                    rootTestFolder,
                    GetCommonTestAssemblyFolderEnding(),
                    "MongoDB.Driver.Core.dll");
                testAssemblyCodeBaseUri = new Uri(assemblyPath).ToString();
            }

            var subject = new TestRelativeLibraryLocator(testAssemblyCodeBaseUri);

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
            private readonly string _testAssemblyUri;

            public TestRelativeLibraryLocator(string testAssemblyUri)
            {
                _testAssemblyUri = testAssemblyUri; // can be null
            }

            public override string GetBaseAssemblyUri() => _testAssemblyUri ??  base.GetBaseAssemblyUri();

            public override string GetLibraryRelativePath(OperatingSystemPlatform currentPlatform) => throw new NotImplementedException();
        }
    }
}
