/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver.Communication.Security
{
    /// <summary>
    /// A high-level sasl conversation object.
    /// </summary>
    internal class SaslConversation : IDisposable
    {
        // private fields
        private bool _isDisposed;
        private List<IDisposable> _itemsNeedingDisposal;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslConversation" /> class.
        /// </summary>
        public SaslConversation()
        {
            _itemsNeedingDisposal = new List<IDisposable>();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SaslConversation" /> class.
        /// </summary>
        ~SaslConversation()
        {
            Dispose(false);
        }

        // public methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Registers the item for disposal.
        /// </summary>
        /// <param name="disposable">The disposable.</param>
        public void RegisterItemForDisposal(IDisposable disposable)
        {
            _itemsNeedingDisposal.Add(disposable);
        }

        // private methods
        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                // disposal should happen in reverse order of registration.
                if (disposing && _itemsNeedingDisposal != null)
                {
                    for (int i = _itemsNeedingDisposal.Count - 1; i >= 0; i--)
                    {
                        _itemsNeedingDisposal[i].Dispose();
                    }

                    _itemsNeedingDisposal.Clear();
                    _itemsNeedingDisposal = null;
                }

                _isDisposed = true;
            }
        }
    }
}