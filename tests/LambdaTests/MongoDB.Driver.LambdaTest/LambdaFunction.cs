using System;
using System.Collections.Generic;
using System.Text.Json;
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

        public async Task<APIGatewayProxyResponse> LambdaFunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            var col = _mongoClient.GetDatabase("lambdaTest").GetCollection<BsonDocument>("test");
            var testDoc = new BsonDocument("n", 1);
            await col.InsertOneAsync(testDoc);
            await col.DeleteOneAsync(doc => doc["_id"] == testDoc["_id"]);

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
                    { "CommandCount", $"{_totalCommandCount}" },
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
            if (@event.Awaited && !_serverHeartbeatWasAwaited)
            {
                _serverHeartbeatWasAwaited = true;
            }
        }

        private void Handle(ServerHeartbeatFailedEvent @event)
        {
            _totalHeartbeatCount++;
            _totalHeartbeatDurationMs += @event.Duration.TotalMilliseconds;
            if (@event.Awaited && !_serverHeartbeatWasAwaited)
            {
                _serverHeartbeatWasAwaited = true;
            }
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
            _openConnections--;
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
