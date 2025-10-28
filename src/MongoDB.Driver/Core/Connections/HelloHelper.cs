/* Copyright 2010–present MongoDB Inc.
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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Connections
{
    internal static class HelloHelper
    {
        internal static BsonDocument AddClientDocumentToCommand(BsonDocument command, BsonDocument clientDocument)
        {
            return command.Add("client", clientDocument, clientDocument != null);
        }

        internal static BsonDocument AddCompressorsToCommand(BsonDocument command, IEnumerable<CompressorConfiguration> compressors)
        {
            var compressorsArray = new BsonArray(compressors.Select(x => CompressorTypeMapper.ToServerName(x.Type)));

            return command.Add("compression", compressorsArray);
        }

        internal static BsonDocument CreateCommand(ServerApi serverApi, bool helloOk = false, TopologyVersion topologyVersion = null, TimeSpan? maxAwaitTime = null, bool loadBalanced = false)
        {
            Ensure.That(
                (topologyVersion == null && !maxAwaitTime.HasValue) ||
                (topologyVersion != null && maxAwaitTime.HasValue),
                $"Both {nameof(topologyVersion)} and {nameof(maxAwaitTime)} must be filled or null.");

            var helloCommandName = helloOk || loadBalanced || serverApi != null ? "hello" : OppressiveLanguageConstants.LegacyHelloCommandName;
            return new BsonDocument
            {
                { helloCommandName, 1 },
                { "helloOk", true },
                { "topologyVersion", () => topologyVersion.ToBsonDocument(), topologyVersion != null },
                { "maxAwaitTimeMS", () => (long)maxAwaitTime.Value.TotalMilliseconds, maxAwaitTime.HasValue },
                { "loadBalanced", true, loadBalanced }
            };
        }

        internal static BsonDocument CustomizeCommand(OperationContext operationContext, BsonDocument command, IAuthenticator authenticator)
        {
            return authenticator != null
                ? authenticator.CustomizeInitialHelloCommand(operationContext, command)
                : command;
        }

        internal static CommandWireProtocol<BsonDocument> CreateProtocol(
            BsonDocument helloCommand,
            ServerApi serverApi,
            CommandResponseHandling commandResponseHandling = CommandResponseHandling.Return)
        {
            return new CommandWireProtocol<BsonDocument>(
                databaseNamespace: DatabaseNamespace.Admin,
                command: helloCommand,
                secondaryOk: true,
                commandResponseHandling: commandResponseHandling,
                resultSerializer: BsonDocumentSerializer.Instance,
                messageEncoderSettings: null,
                serverApi,
                serializationDomain: BsonSerializer.DefaultSerializationDomain); //Here and in similar cases using the default serialization domain is ok because we only serialize/deserialize BsonDocuments
        }

        internal static HelloResult GetResult(
            OperationContext operationContext,
            IConnection connection,
            CommandWireProtocol<BsonDocument> helloProtocol)
        {
            try
            {
                var helloResultDocument = helloProtocol.Execute(operationContext, connection);
                return new HelloResult(helloResultDocument);
            }
            catch (MongoCommandException ex) when (ex.Code == 11)
            {
                // If the hello or legacy hello command fails with error code 11 (UserNotFound), drivers must consider authentication
                // to have failed.In such a case, drivers MUST raise an error that is equivalent to what they would have
                // raised if the authentication mechanism were specified and the server responded the same way.
                throw new MongoAuthenticationException(connection.ConnectionId, "User not found.", ex);
            }
        }

        internal static async Task<HelloResult> GetResultAsync(
            OperationContext operationContext,
            IConnection connection,
            CommandWireProtocol<BsonDocument> helloProtocol)
        {
            try
            {
                var helloResultDocument = await helloProtocol.ExecuteAsync(operationContext, connection).ConfigureAwait(false);
                return new HelloResult(helloResultDocument);
            }
            catch (MongoCommandException ex) when (ex.Code == 11)
            {
                // If the hello or legacy hello command fails with error code 11 (UserNotFound), drivers must consider authentication
                // to have failed.In such a case, drivers MUST raise an error that is equivalent to what they would have
                // raised if the authentication mechanism were specified and the server responded the same way.
                throw new MongoAuthenticationException(connection.ConnectionId, "User not found.", ex);
            }
        }
    }
}
