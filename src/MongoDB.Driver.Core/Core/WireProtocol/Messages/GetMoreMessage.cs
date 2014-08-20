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

using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public class GetMoreMessage : RequestMessage, IEncodableMessage<GetMoreMessage>
    {
        // fields
        private readonly int _batchSize;
        private readonly string _collectionName;
        private readonly long _cursorId;
        private readonly string _databaseName;

        // constructors
        public GetMoreMessage(
            int requestId,
            string databaseName,
            string collectionName,
            long cursorId,
            int batchSize)
            : base(requestId)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _cursorId = cursorId;
            _batchSize = Ensure.IsGreaterThanOrEqualToZero(batchSize, "batchSize");
        }

        // properties
        public int BatchSize
        {
            get { return _batchSize; }
        }

        public string CollectionName
        {
            get { return _collectionName; }
        }

        public long CursorId
        {
            get { return _cursorId; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        // methods
        public new IMessageEncoder<GetMoreMessage> GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetGetMoreMessageEncoder();
        }

        protected override IMessageEncoder GetNonGenericEncoder(IMessageEncoderFactory encoderFactory)
        {
            return GetEncoder(encoderFactory);
        }
    }
}
