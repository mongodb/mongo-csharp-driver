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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    public class CollectionExistsOperation : IReadOperation<bool>
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;

        // constructors
        public CollectionExistsOperation(
            string databaseName,
            string collectionName)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
        }

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        // methods
        public async Task<bool> ExecuteAsync(IReadBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var operation = new ListCollectionNamesOperation(_databaseName);
            var collectionNames = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            return collectionNames.Contains(_collectionName);
        }

        public CollectionExistsOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new CollectionExistsOperation(_databaseName, value);
        }

        public CollectionExistsOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new CollectionExistsOperation(value, _collectionName);
        }
    }
}
