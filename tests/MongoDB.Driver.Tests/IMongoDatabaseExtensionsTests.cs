/* Copyright 2018-present MongoDB Inc.
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

using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class IMongoDatabaseExtensionsTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Watch_should_call_client_with_expected_arguments(
            [Values(false, true)] bool usingSession,
            [Values(false, true)] bool async)
        {
            var mockDatabase = new Mock<IMongoDatabase>();
            var database = mockDatabase.Object;
            var session = new Mock<IClientSessionHandle>().Object;
            var options = new ChangeStreamOptions();
            var cancellationToken = new CancellationTokenSource().Token;

            if (usingSession)
            {
                if (async)
                {
                    database.WatchAsync(session, options, cancellationToken);
                    mockDatabase.Verify(m => m.WatchAsync(session, It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    database.Watch(session, options, cancellationToken);
                    mockDatabase.Verify(m => m.Watch(session, It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>>(), options, cancellationToken), Times.Once);
                }
            }
            else
            {
                if (async)
                {
                    database.WatchAsync(options, cancellationToken);
                    mockDatabase.Verify(m => m.WatchAsync(It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    database.Watch(options, cancellationToken);
                    mockDatabase.Verify(m => m.Watch(It.IsAny<EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>>(), options, cancellationToken), Times.Once);
                }
            }
        }
    }
}
