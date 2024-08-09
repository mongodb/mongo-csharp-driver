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
using System.Net;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication
{
    /// <summary>
    /// Represents a SASL conversation.
    /// </summary>
    public sealed class SaslConversation : IDisposable
    {
        // fields
        private readonly List<IDisposable> _itemsNeedingDisposal;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaslConversation"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="endPoint">The connection remote EndPoint</param>
        public SaslConversation(ConnectionId connectionId, EndPoint endPoint)
        {
            ConnectionId = connectionId;
            EndPoint = endPoint;
            _itemsNeedingDisposal = new List<IDisposable>();
        }

        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        public ConnectionId ConnectionId { get; }

        /// <summary>
        /// Gets the connection remote EndPoint.
        /// </summary>
        public EndPoint EndPoint { get; }

        /// <summary>
        /// Registers the item for disposal.
        /// </summary>
        /// <param name="item">The disposable item.</param>
        public void RegisterItemForDisposal(IDisposable item)
        {
            Ensure.IsNotNull(item, nameof(item));
            _itemsNeedingDisposal.Add(item);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                for (int i = _itemsNeedingDisposal.Count - 1; i >= 0; i--)
                {
                    _itemsNeedingDisposal[i].Dispose();
                }

                _itemsNeedingDisposal.Clear();
                _isDisposed = true;
            }
        }
    }
}
