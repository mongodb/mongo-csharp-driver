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
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class UpdateWireProtocol : WriteWireProtocolBase
    {
        // fields
        private readonly bool _isMulti;
        private readonly bool _isUpsert;
        private readonly BsonDocument _query;
        private readonly BsonDocument _update;

        // constructors
        public UpdateWireProtocol(
            string databaseName,
            string collectionName,
            WriteConcern writeConcern,
            BsonDocument query,
            BsonDocument update,
            bool isMulti,
            bool isUpsert)
            : base(databaseName, collectionName, writeConcern)
        {
            _query = Ensure.IsNotNull(query, "query");
            _update = Ensure.IsNotNull(update, "update");
            _isMulti = isMulti;
            _isUpsert = isUpsert;
        }

        // methods
        protected override RequestMessage CreateWriteMessage(IConnection connection)
        {
            return new UpdateMessage(
                RequestMessage.GetNextRequestId(),
                DatabaseName,
                CollectionName,
                _query,
                _update,
                _isMulti,
                _isUpsert);
        }
    }
}
