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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Operations
{
    public class DeleteOpcodeOperationEmulator
    {
        // fields
        private string _collectionName;
        private string _databaseName;
        private bool _isMulti;
        private BsonDocument _query;
        private WriteConcern _writeConcern;

        // constructors
        public DeleteOpcodeOperationEmulator(
            string databaseName,
            string collectionName,
            BsonDocument query)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _query = Ensure.IsNotNull(query, "query");
            _writeConcern = WriteConcern.Acknowledged;
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
            set { _collectionName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        public bool IsMulti
        {
            get { return _isMulti; }
            set { _isMulti = value; }
        }

        public BsonDocument Query
        {
            get { return _query; }
            set { _query = Ensure.IsNotNull(value, "value"); }
        }

        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set { _writeConcern = Ensure.IsNotNull(value, "value"); }
        }

        // methods
        public async Task<BsonDocument> ExecuteAsync(IConnectionHandle connection, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(connection, "connection");

            var limit = _isMulti ? 0 : 1;
            var requests = new[] { new DeleteRequest(_query) { Limit = limit } };

            var operation = new BulkDeleteOperation(_databaseName, _collectionName, requests)
            {
                WriteConcern = _writeConcern
            };

            BulkWriteResult bulkWriteResult;
            BulkWriteException bulkWriteException = null;
            try
            {
                bulkWriteResult = await operation.ExecuteAsync(connection, timeout, cancellationToken);
            }
            catch (BulkWriteException ex)
            {
                bulkWriteResult = ex.Result;
                bulkWriteException = ex;
            }

            var converter = new BulkWriteResultConverter();
            if (bulkWriteException != null)
            {
                throw converter.ToWriteConcernException(bulkWriteException);
            }
            else
            {
                if (_writeConcern.IsAcknowledged)
                {
                    return converter.ToWriteConcernResult(bulkWriteResult).Response;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
