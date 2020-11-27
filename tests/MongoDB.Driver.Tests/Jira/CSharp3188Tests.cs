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

using System;
using System.IO;
using System.Net.Sockets;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3188Tests
    {
        [SkippableTheory]
        [ParameterAttributeData]
        public void Connection_timeout_should_throw_expected_exception([Values(false, true)] bool async)
        {
            RequireServer
                .Check()
                .Supports(Feature.AggregateFunction)
                .ClusterTypes(ClusterType.Standalone, ClusterType.ReplicaSet); // server sleep doesn't correctly work with sharded

            var socketTimeout = TimeSpan.FromMilliseconds(100);
            var clientSettings = DriverTestConfiguration.GetClientSettings().Clone();
            clientSettings.SocketTimeout = socketTimeout;

            using (var client = DriverTestConfiguration.CreateDisposableClient(clientSettings))
            {
                var database = client.GetDatabase("db");
                var collection = database.GetCollection<BsonDocument>("coll");

                var serverResponseDelay = socketTimeout + TimeSpan.FromMilliseconds(500);
                var stringFieldDefinition = $@"
                {{
                    done : {{
                        $function : {{
                            body : 'function() {{ sleep({serverResponseDelay.TotalMilliseconds}); return true }}',
                            args : [ ],
                            lang : 'js'
                        }}
                    }}
                }}";
                var projectionDefinition = Builders<BsonDocument>
                    .Projection
                    .Combine(BsonDocument.Parse(stringFieldDefinition));
                var pipeline = new EmptyPipelineDefinition<BsonDocument>()
                    .AppendStage<BsonDocument, BsonDocument, BsonDocument>("{ $collStats : { } }")
                    .Limit(1)
                    .Project(projectionDefinition);

                if (async)
                {
                    var exception = Record.Exception(() => collection.AggregateAsync(pipeline).GetAwaiter().GetResult());

                    var mongoConnectionException = exception.Should().BeOfType<MongoConnectionException>().Subject;
#pragma warning disable CS0618 // Type or member is obsolete
                    mongoConnectionException.ContainsSocketTimeoutException.Should().BeFalse();
#pragma warning restore CS0618 // Type or member is obsolete
                    mongoConnectionException.ContainsTimeoutException.Should().BeTrue();
                    mongoConnectionException
                        .InnerException.Should().BeOfType<TimeoutException>().Subject
                        .InnerException.Should().BeNull();
                }
                else
                {
                    var exception = Record.Exception(() => collection.Aggregate(pipeline));

                    var mongoConnectionException = exception.Should().BeOfType<MongoConnectionException>().Subject;
#pragma warning disable CS0618 // Type or member is obsolete
                    mongoConnectionException.ContainsSocketTimeoutException.Should().BeTrue();
#pragma warning restore CS0618 // Type or member is obsolete
                    mongoConnectionException.ContainsTimeoutException.Should().BeTrue();
                    var socketException = mongoConnectionException
                        .InnerException.Should().BeOfType<IOException>().Subject
                        .InnerException.Should().BeOfType<SocketException>().Subject;
                    socketException.SocketErrorCode.Should().Be(SocketError.TimedOut);
                    socketException.InnerException.Should().BeNull();
                }
            }
        }
    }
}
