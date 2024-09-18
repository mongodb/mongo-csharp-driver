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
using System.Diagnostics;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Libmongocrypt;

namespace MongoDB.Driver.SmokeTests.Sdk
{
    internal static class InfrastructureUtilities
    {
        public static void ValidateMongoDBPackageVersion(string package)
        {
            var packageShaExpected = Environment.GetEnvironmentVariable("SmokeTestsPackageSha");

            if (!string.IsNullOrEmpty(packageShaExpected))
            {
                var fileVersionInfo = package switch
                {
                    "driver" => FileVersionInfo.GetVersionInfo(typeof(MongoClient).Assembly.Location),
                    "libmongocrypt" => FileVersionInfo.GetVersionInfo(typeof(ClientEncryption).Assembly.Location),
                    _ => throw new ArgumentOutOfRangeException(nameof(package), package, "Unknown package used in smoke tests.")
                };

                fileVersionInfo.ProductVersion.Contains(packageShaExpected)
                    .Should().BeTrue("Expected package sha {0} in {1}", packageShaExpected, fileVersionInfo.ProductVersion);
            }
        }
    }
}
