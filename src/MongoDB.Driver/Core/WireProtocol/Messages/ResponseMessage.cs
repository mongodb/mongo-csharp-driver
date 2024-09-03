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

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    internal abstract class ResponseMessage : MongoDBMessage
    {
        // fields
        private readonly int _requestId;
        private readonly int _responseTo;

        // constructors
        protected ResponseMessage(int requestId, int responseTo)
        {
            _requestId = requestId;
            _responseTo = responseTo;
        }

        // properties
        public override MongoDBMessageType MessageType
        {
            get { return MongoDBMessageType.Reply; }
        }

        public int RequestId
        {
            get { return _requestId; }
        }

        public int ResponseTo
        {
            get { return _responseTo; }
        }
    }
}
