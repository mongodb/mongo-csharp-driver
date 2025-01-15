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
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests
{
    [IntegrationTest]
    public abstract class IntegrationTest<TFixture> : LoggableTestClass, IClassFixture<TFixture>
        where TFixture : MongoDatabaseFixture
    {
        private readonly TFixture _fixture;

        protected IntegrationTest(ITestOutputHelper testOutput, TFixture fixture, Action<RequireServer> requireServerCheck = null)
            : base(testOutput, fixture.LogsAccumulator)
        {
            _fixture = fixture;
            requireServerCheck?.Invoke(RequireServer.Check());
            _fixture.Initialize();
        }

        public TFixture Fixture => _fixture;
    }
}
