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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests
{
    [IntegrationTest]
    public abstract class IntegrationTest<TFixture> : LoggableTestClass, IClassFixture<TFixture>
        where TFixture : DatabaseFixture
    {
        protected IntegrationTest(ITestOutputHelper testOutputHelper, TFixture fixture)
            : base(testOutputHelper)
        {
            Fixture = fixture;
        }

        protected TFixture Fixture { get; }

        public IMongoClient GetMongoClient(Action<MongoClientSettings> configure = null)
        {
            return Fixture.GetMongoClient(settings =>
            {
                settings.LoggingSettings = LoggingSettings;
                configure?.Invoke(settings);
            });
        }

        public IMongoDatabase GetDatabase(Action<MongoClientSettings> configure = null)
        {
            return Fixture.GetDatabase(settings =>
            {
                settings.LoggingSettings = LoggingSettings;
                configure?.Invoke(settings);
            });
        }

        public IMongoCollection<T> GetCollection<T>(Action<MongoClientSettings> configure = null, string collectionName = null)
        {
            return Fixture.GetCollection<T>(settings =>
            {
                settings.LoggingSettings = LoggingSettings;
                configure?.Invoke(settings);
            }, collectionName);
        }
    }
}
