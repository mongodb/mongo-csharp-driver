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
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace MongoDB.Driver.LambdaTest
{
    public class LambdaFunction
    {
        private MongoClient _mongoClient;
        private long _openConnections;
        private long _totalHeartbeatCount;
        private double _totalHeartbeatDurationMs;
        private long _totalCommandCount;
        private double _totalCommandDurationMs;
        private bool _serverHeartbeatWasAwaited;

        public LambdaFunction()
        {
            _openConnections = 0;
            _totalHeartbeatCount = 0;
            _totalHeartbeatDurationMs = 0;
            _totalCommandCount = 0;
            _totalCommandDurationMs = 0;
            _serverHeartbeatWasAwaited = false;

            var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");

            // register listeners for the to be monitored events
            var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
            clientSettings.ClusterConfigurator = builder =>
            {
                builder
                    .Subscribe<ServerHeartbeatSucceededEvent>(Handle)
                    .Subscribe<ServerHeartbeatFailedEvent>(Handle)
                    .Subscribe<CommandSucceededEvent>(Handle)
                    .Subscribe<CommandFailedEvent>(Handle)
                    .Subscribe<ConnectionCreatedEvent>(Handle)
                    .Subscribe<ConnectionClosedEvent>(Handle);
            };

            // Client is used for all requests
            _mongoClient = new MongoClient(clientSettings);
        }

        public async Task<APIGatewayProxyResponse> LambdaFunctionHandlerAsync(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            var collection = _mongoClient.GetDatabase("lambdaTest").GetCollection<BsonDocument>("test");
            var testDocument = new BsonDocument("n", 1);
            await collection.InsertOneAsync(testDocument);
            await collection.DeleteOneAsync(doc => doc["_id"] == testDocument["_id"]);

            Dictionary<string, string> responseBody;
            if (_serverHeartbeatWasAwaited)
            {
                // This is an error since the streaming protocol should be disabled in a FaaS environment thus no server heartbeat should be awaited
                responseBody = new Dictionary<string, string>
                {
                    { "message", "ERROR: no ServerHeartBeat events should contain the awaited=true flag." },
                };
            }
            else
            {
                responseBody = new Dictionary<string, string>
                {
                    { "averageCommandDurationMS", $"{_totalCommandDurationMs / _totalCommandCount :F3}"},
                    { "averageServerHeartbeatDurationMS", $"{_totalHeartbeatDurationMs / _totalHeartbeatCount :F3}"},
                    { "openConnections", $"{_openConnections}" },
                    { "heartBeatCount", $"{_totalHeartbeatCount}" },
                    { "commandCount", $"{_totalCommandCount}" },
                };
            }

            Reset();

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(responseBody, new JsonSerializerOptions { WriteIndented = true}),
                StatusCode = _serverHeartbeatWasAwaited ? 502 : 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
        }

        // actions for monitored events
        private void Handle(ServerHeartbeatSucceededEvent @event)
        {
            _totalHeartbeatCount++;
            _totalHeartbeatDurationMs += @event.Duration.TotalMilliseconds;
            _serverHeartbeatWasAwaited |= @event.Awaited;
        }

        private void Handle(ServerHeartbeatFailedEvent @event)
        {
            _totalHeartbeatCount++;
            _totalHeartbeatDurationMs += @event.Duration.TotalMilliseconds;
            _serverHeartbeatWasAwaited |= @event.Awaited;
        }

        private void Handle(CommandSucceededEvent @event)
        {
            _totalCommandCount++;
            _totalCommandDurationMs += @event.Duration.TotalMilliseconds;
        }

        private void Handle(CommandFailedEvent @event)
        {
            _totalCommandCount++;
            _totalCommandDurationMs += @event.Duration.TotalMilliseconds;
        }

        private void Handle(ConnectionCreatedEvent @event)
        {
            _openConnections++;
        }

        private void Handle(ConnectionClosedEvent @event)
        {
            Interlocked.Decrement(ref _openConnections);
        }

        private void Reset()
        {
             _totalHeartbeatCount = 0;
             _totalHeartbeatDurationMs = 0;
             _totalCommandCount = 0;
             _totalCommandDurationMs = 0;
        }
    }
}
