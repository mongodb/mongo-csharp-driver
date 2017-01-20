/* Copyright 2013-2016 MongoDB Inc.
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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class WriteConcernErrorHelperTests
    {
        [Fact]
        public void ThrowIfHasWriteConcernError_should_not_throw_when_there_is_no_write_concern_error()
        {
            var connectionId = CreateConnectionId();
            var result = BsonDocument.Parse("{ ok : 1 }");

            WriteConcernErrorHelper.ThrowIfHasWriteConcernError(connectionId, result);
        }

        [Fact]
        public void ThrowIfHasWriteConcernError_should_throw_when_there_is_a_write_concern_error()
        {
            var connectionId = CreateConnectionId();
            var result = BsonDocument.Parse("{ ok : 1, writeConcernError : { errmsg : 'message' } }");

            var exception = Record.Exception(() => WriteConcernErrorHelper.ThrowIfHasWriteConcernError(connectionId, result));

            var writeConcernException = exception.Should().BeOfType<MongoWriteConcernException>().Subject;
            writeConcernException.ConnectionId.Should().BeSameAs(connectionId);
            writeConcernException.Message.Should().Be(result["writeConcernError"]["errmsg"].AsString);
            var writeConcernResult = writeConcernException.WriteConcernResult;
            writeConcernResult.Response.Should().BeSameAs(result);
        }

        // private methods
        private ConnectionId CreateConnectionId()
        {
            var clusterId = new ClusterId(1);
            var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
            return new ConnectionId(serverId, 2);
        }
    }
}
