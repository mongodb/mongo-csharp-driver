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
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.TestHelpers
{
    public static class CoreExceptionHelper
    {
        public static Exception CreateException(Type exceptionType)
        {
            switch (exceptionType.Name)
            {
                case "IOException":
                    return new IOException("Fake IOException.");

                case "MongoConnectionException":
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        var message = "Fake MongoConnectionException";
                        var innerException = new Exception();
                        return new MongoConnectionException(connectionId, message, innerException);
                    }

                case "MongoConnectionClosedException":
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        return new MongoConnectionClosedException(connectionId);
                    }

                case "MongoCursorNotFoundException":
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        var cursorId = 1L;
                        var query = new BsonDocument();
                        return new MongoCursorNotFoundException(connectionId, cursorId, query);
                    }

                case "MongoNodeIsRecoveringException":
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        var result = new BsonDocument();
                        return new MongoNodeIsRecoveringException(connectionId, null, result);
                    }

                case "MongoNotPrimaryException":
                    {
                        var clusterId = new ClusterId(1);
                        var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
                        var connectionId = new ConnectionId(serverId, 1);
                        var result = new BsonDocument();
                        return new MongoNotPrimaryException(connectionId, null, result);
                    }

                default:
                    throw new ArgumentException($"Unexpected exception type: {exceptionType.Name}.", nameof(exceptionType));
            }
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
