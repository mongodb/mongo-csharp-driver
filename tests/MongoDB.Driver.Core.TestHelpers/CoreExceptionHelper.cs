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

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.TestHelpers
{
    public static class CoreExceptionHelper
    {
        public static Exception CreateException(string errorType)
        {
            switch (errorType)
            {
                // Exception Types first:
                case nameof(IOException):
                    return new IOException("Fake IOException.");

                case nameof(MongoConnectionException):
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        var message = "Fake MongoConnectionException";
                        var innerException = new Exception();
                        return new MongoConnectionException(connectionId, message, innerException);
                    }

                case nameof(MongoConnectionClosedException):
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        return new MongoConnectionClosedException(connectionId);
                    }

                case nameof(MongoCursorNotFoundException):
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        var cursorId = 1L;
                        var query = new BsonDocument();
                        return new MongoCursorNotFoundException(connectionId, cursorId, query);
                    }

                case nameof(MongoNodeIsRecoveringException):
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        var result = new BsonDocument();
                        return new MongoNodeIsRecoveringException(connectionId, null, result);
                    }

                case nameof(MongoNotPrimaryException):
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        var result = new BsonDocument();
                        return new MongoNotPrimaryException(connectionId, null, result);
                    }

                // custom errors next:
                case "IOExceptionWithNetworkUnreachableSocketException":
                    {
                        var innerException = CreateException("NetworkUnreachableSocketException");
                        return new IOException("IoExceptionWithNetworkUnreachableException", innerException);
                    }

                case "IOExceptionWithTimedOutSocketException":
                    {
                        var innerException = CreateException("TimedOutSocketException");
                        return new IOException("IOExceptionWithTimedOutSocketException", innerException);
                    }

                case "NetworkUnreachableSocketException":
                    return new SocketException((int)SocketError.NetworkUnreachable);

                case "TimedOutSocketException":
                    return new SocketException((int)SocketError.TimedOut);

                case "ConnectionRefusedSocketException":
                    return new SocketException((int)SocketError.ConnectionRefused);

                case nameof(MongoConnectionPoolPausedException):
                    return new MongoConnectionPoolPausedException("MongoConnectionPoolPausedException");

                default:
                    throw new ArgumentException("Unknown error type.");
            }
        }

        public static Exception CreateException(Type exceptionType)
        {
            return CreateException(exceptionType.Name);
        }

        public static MongoCommandException CreateMongoCommandException(int code = 1, string label = null)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var connectionId = new ConnectionId(serverId);
            var message = "Fake MongoCommandException";
            var command = BsonDocument.Parse("{ command : 1 }");
            var result = BsonDocument.Parse($"{{ ok: 0, code : {code} }}");
            var commandException = new MongoCommandException(connectionId, message, command, result);
            if (label != null)
            {
                commandException.AddErrorLabel(label);
            }

            return commandException;
        }

        public static MongoCommandException CreateMongoWriteConcernException(BsonDocument writeConcernResultDocument, string label = null)
        {
            var clusterId = new ClusterId(1);
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(clusterId, endPoint);
            var connectionId = new ConnectionId(serverId);
            var message = "Fake MongoWriteConcernException";
            var writeConcernResult = new WriteConcernResult(writeConcernResultDocument);
            var writeConcernException = new MongoWriteConcernException(connectionId, message, writeConcernResult);
            if (label != null)
            {
                writeConcernException.AddErrorLabel(label);
            }

            return writeConcernException;
        }

    }
}
