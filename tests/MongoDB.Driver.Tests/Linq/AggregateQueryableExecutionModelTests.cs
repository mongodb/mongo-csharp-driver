/* Copyright 2020-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class AggregateQueryableExecutionModelTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Execute_should_call_Aggregate_with_expected_arguments(
            [Values(false, true)] bool withSession,
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var mockCollection = CreateMockCollection();
            var session = withSession ? Mock.Of<IClientSessionHandle>() : null;
            var options = new AggregateOptions();
            var cancellationToken = new CancellationToken();

            if (async)
            {
                _ = subject.ExecuteAsync(mockCollection.Object, session, options, cancellationToken);

                if (withSession)
                {
                    mockCollection.Verify(m => m.AggregateAsync(session, It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(), options, cancellationToken), Times.Once);
                }
                else
                {
                    mockCollection.Verify(m => m.AggregateAsync(It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(), options, cancellationToken), Times.Once);
                }
            }
            else
            {
                _ = subject.Execute(mockCollection.Object, session, options);

                if (withSession)
                {
                    mockCollection.Verify(m => m.Aggregate(session, It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(), options, CancellationToken.None), Times.Once);
                }
                else
                {
                    mockCollection.Verify(m => m.Aggregate(It.IsAny<PipelineDefinition<BsonDocument, BsonDocument>>(), options, CancellationToken.None), Times.Once);
                }
            }
        }

        // private methods
        private Mock<IMongoCollection<BsonDocument>> CreateMockCollection()
        {
            return new Mock<IMongoCollection<BsonDocument>>();
        }

        private AggregateQueryableExecutionModel<BsonDocument> CreateSubject()
        {
            var stages = new BsonDocument[0];
            var outputSerializer = BsonDocumentSerializer.Instance;
            return new AggregateQueryableExecutionModel<BsonDocument>(stages, outputSerializer);
        }
    }
}
