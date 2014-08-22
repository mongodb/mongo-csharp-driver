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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class DropDatabaseOperation : IWriteOperation<BsonDocument>, ICommandOperation
    {
        // fields
        private string _databaseName;

        // constructors
        public DropDatabaseOperation(string databaseName)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
        }

        // properties
        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = Ensure.IsNotNullOrEmpty(value, "value"); }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument { { "dropDatabase", 1 } };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation(_databaseName, command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }
    }
}
