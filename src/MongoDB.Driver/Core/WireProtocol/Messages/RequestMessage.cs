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
using System.Threading;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    internal abstract class RequestMessage : MongoDBMessage
    {
        #region static
        // static fields
        private static int __requestId;

        // static properties
        public static int CurrentGlobalRequestId
        {
            get { return __requestId; }
        }

        // static methods
        public static int GetNextRequestId()
        {
            return Interlocked.Increment(ref __requestId);
        }
        #endregion

        // fields
        private readonly int _requestId;
        private readonly Func<bool> _shouldBeSent;
        private bool _wasSent;

        // constructors
        protected RequestMessage(int requestId, Func<bool> shouldBeSent = null)
        {
            _requestId = requestId;
            _shouldBeSent = shouldBeSent;
        }

        // properties
        public int RequestId
        {
            get { return _requestId; }
        }

        public Func<bool> ShouldBeSent
        {
            get { return _shouldBeSent; }
        }

        public bool WasSent
        {
            get { return _wasSent; }
            set { _wasSent = value; }
        }
    }
}
