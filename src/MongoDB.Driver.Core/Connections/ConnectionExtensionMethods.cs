/* Copyright 2013-2014 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents internal IConnection extension methods (used to easily access the IConnectionInternal methods).
    /// </summary>
    internal static class ConnectionExtensionMethods
    {
        // static methods
        public static Task<ReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(this IConnection connection, int responseTo, IBsonSerializer<TDocument> serializer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");
            return ((IConnectionInternal)connection).ReceiveMessageAsync<TDocument>(responseTo, serializer, timeout, cancellationToken);
        }

        public static Task SendMessageAsync(this IConnection connection, RequestMessage message, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");
            return ((IConnectionInternal)connection).SendMessagesAsync(new[] { message }, timeout, cancellationToken);
        }

        public static Task SendMessagesAsync(this IConnection connection, IEnumerable<RequestMessage> messages, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(connection, "connection");
            return ((IConnectionInternal)connection).SendMessagesAsync(messages, timeout, cancellationToken);
        }
    }
}
