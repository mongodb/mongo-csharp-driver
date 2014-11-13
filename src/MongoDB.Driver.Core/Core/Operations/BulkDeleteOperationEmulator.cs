﻿/* Copyright 2010-2014 MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    internal class BulkDeleteOperationEmulator : BulkUnmixedWriteOperationEmulatorBase
    {
        // constructors
        public BulkDeleteOperationEmulator(
            CollectionNamespace collectionNamespace,
            IEnumerable<DeleteRequest> requests,
            MessageEncoderSettings messageEncoderSettings)
            : base(collectionNamespace, requests, messageEncoderSettings)
        {
        }

        // methods
        protected override IWireProtocol<WriteConcernResult> CreateProtocol(IConnectionHandle connection, WriteRequest request)
        {
            var deleteRequest = (DeleteRequest)request;
            var isMulti = deleteRequest.Limit == 0;

            return new DeleteWireProtocol(
               CollectionNamespace,
               deleteRequest.Criteria,
               isMulti,
               MessageEncoderSettings,
               WriteConcern);
        }
    }
}
