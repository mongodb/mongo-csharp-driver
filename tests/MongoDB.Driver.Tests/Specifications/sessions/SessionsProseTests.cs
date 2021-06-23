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

using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.sessions
{
    public class SessionsProseTests
    {
        [SkippableFact]
        public void Snapshot_and_causal_consistent_session_is_not_allowed()
        {
            var minVersion = new SemanticVersion(3, 6, 0, "");
            RequireServer.Check().VersionGreaterThanOrEqualTo(minVersion);

            var sessionOptions = new ClientSessionOptions()
            {
                Snapshot = true,
                CausalConsistency = true
            };

            var mongoClient = DriverTestConfiguration.Client;

            var exception = Record.Exception(() => mongoClient.StartSession(sessionOptions));
            exception.Should().BeOfType<MongoClientException>();
        }
    }
}
