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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.UnifiedTestOperations;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption
{
    [Trait("Category", "CSFLE")]
    public sealed class ClientSideEncryptionUnifiedTestRunner : LoggableTestClass
    {
        // public constructors
        public ClientSideEncryptionUnifiedTestRunner(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        // public methods
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            var testCaseNameLower = testCase.Name.ToLower();

            if (testCaseNameLower.Contains("rewrap with"))
            {
                RequirePlatform // rewrap tests calls gcp kms that is supported starting from netstandard2.1
                    .Check()
                    .SkipWhen(SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20)
                    .SkipWhen(SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20);
            }

            if (testCaseNameLower.Contains("kmip") ||
                testCaseNameLower.Contains("rewrap with current kms provider")) // also calls kmip kms
            {
                // kmip requires configuring kms mock server
                RequireEnvironment.Check().EnvironmentVariable("KMS_MOCK_SERVERS_ENABLED");
            }

            RequirePlatform
                .Check()
                .SkipWhen(() => testCaseNameLower.Contains("gcp"), SupportedOperatingSystem.Linux, SupportedTargetFramework.NetStandard20) // gcp is supported starting from netstandard2.1
                .SkipWhen(() => testCaseNameLower.Contains("gcp"), SupportedOperatingSystem.MacOS, SupportedTargetFramework.NetStandard20); // gcp is supported starting from netstandard2.1

            using (var runner = new UnifiedTestRunner())
            {
                runner.Run(testCase);
            }
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.client_side_encryption.tests.unified.";

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                foreach (var testCase in base.CreateTestCases(document))
                {
                    foreach (var async in new[] { false, true })
                    {
                        var name = $"{testCase.Name}:async={async}";
                        var test = testCase.Test.DeepClone().AsBsonDocument.Add("async", async);
                        yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                    }
                }
            }
        }
    }
}
