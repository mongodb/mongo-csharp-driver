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
using System.Reflection;
using FluentAssertions;
using MongoDB.Driver.Encryption;
using Xunit;
using Xunit.Sdk;

namespace MongoDB.Driver.SmokeTests.Sdk
{
    public class ValidatePackagesVersionTests
    {
        private readonly string _packageVersion;

        public ValidatePackagesVersionTests()
        {
            _packageVersion = Environment.GetEnvironmentVariable("DRIVER_PACKAGE_VERSION");
        }

        [Fact]
        public void ValidateDriverPackageVersion() =>
            ValidateAssemblyInformationalVersion(typeof(MongoClient).Assembly, _packageVersion);

        [Fact]
        public void ValidateEncryptionPackageVersion() =>
            ValidateAssemblyInformationalVersion(typeof(ClientEncryption).Assembly, _packageVersion);

        private static void ValidateAssemblyInformationalVersion(Assembly assembly, string expectedVersion)
        {
            if (string.IsNullOrEmpty(expectedVersion))
            {
                return;
            }

            var assemblyInformationAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            assemblyInformationAttribute.Should().NotBeNull();
            assemblyInformationAttribute.InformationalVersion.Should().StartWith(expectedVersion);
        }
    }
}
