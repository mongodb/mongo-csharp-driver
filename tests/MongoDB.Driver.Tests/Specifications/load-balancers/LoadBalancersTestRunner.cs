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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests.UnifiedTestOperations;
using MongoDB.Driver.TestHelpers;
using Xunit;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;

namespace MongoDB.Driver.Tests.Specifications.load_balancers
{
    [Trait("Category", "SupportLoadBalancing")]
    public sealed class LoadBalancersTestRunner
    {
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            ITestClientsProvider testClientsProvider;
#if DEBUG
            RequirePlatform
                .Check()
                .SkipWhen(SupportedOperatingSystem.Linux)
                .SkipWhen(SupportedOperatingSystem.MacOS);
            // Make sure that LB is started. "nginx" is a LB we use for windows testing
            RequireEnvironment.Check().ProcessStarted("nginx");

            ServiceIdHelper.IsServiceIdEmulationEnabled = true;

            var coreEnvironmentConfiguration = new LoadBalancedCoreEnvironmentConfiguration();
            testClientsProvider = new TestClientsProvider(coreEnvironmentConfiguration, ClusterType.LoadBalanced);
            RequireServer
                .ConfigureAndCheck(coreEnvironmentConfiguration)
                .LoadBalancing(enabled: true)
                .Authentication(authentication: false); // auth server requires credentials in connection string
#else
            RequireEnvironment // these env variables are used only on the scripting side
                .Check()
                .EnvironmentVariable("SINGLE_MONGOS_LB_URI")
                .EnvironmentVariable("MULTI_MONGOS_LB_URI");
            // EG currently supports LB only for Ubuntu
            RequirePlatform
                .Check()
                .SkipWhen(SupportedOperatingSystem.Windows)
                .SkipWhen(SupportedOperatingSystem.MacOS);
            testClientsProvider = DriverTestConfiguration.DefaultTestClientsProvider;
#endif

            using (var runner = new UnifiedTestRunner(testClientsProvider: testClientsProvider))
            {
                runner.Run(testCase);
            }
        }

        // nested types
        private class LoadBalancedCoreEnvironmentConfiguration : CoreEnvironmentConfiguration
        {
            protected override ConnectionString GetDefaultConnectionString() => new ConnectionString("mongodb://localhost:17017?loadBalanced=true");
            protected override ConnectionString GetMultipleShardRoutersConnectionString() => new ConnectionString("mongodb://localhost:17018?loadBalanced=true");
        }

        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.load_balancers.tests.";

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
