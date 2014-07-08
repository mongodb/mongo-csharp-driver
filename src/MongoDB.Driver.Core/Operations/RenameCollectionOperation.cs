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
    public class RenameCollectionOperation : IWriteOperation<BsonDocument>
    {
        // fields
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly bool? _dropTarget;
        private readonly string _newCollectionName;
        private readonly string _newDatabaseName;

        // constructors
        public RenameCollectionOperation(
            string databaseName,
            string collectionName,
            string newCollectionName)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _newCollectionName = Ensure.IsNotNullOrEmpty(newCollectionName, "newCollectionName");
        }

        private RenameCollectionOperation(
            string collectionName,
            string databaseName,
            bool? dropTarget,
            string newCollectionName,
            string newDatabaseName)
        {
            _collectionName = collectionName;
            _databaseName = databaseName;
            _dropTarget = dropTarget;
            _newCollectionName = newCollectionName;
            _newDatabaseName = newDatabaseName;
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

        public bool? DropTarget
        {
            get { return _dropTarget; }
        }

        public string NewCollectionName
        {
            get { return _newCollectionName; }
        }

        public string NewDatabaseName
        {
            get { return _newDatabaseName; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument
            {
                { "drop", _databaseName + "." + _collectionName },
                { "to", (_newDatabaseName ?? _databaseName) + "." + _newCollectionName },
                { "dropTarget", () => _dropTarget.Value, _dropTarget.HasValue }
            };
        }

        public async Task<BsonDocument> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new WriteCommandOperation("admin", command);
            return await operation.ExecuteAsync(binding, timeout, cancellationToken);
        }

        public RenameCollectionOperation WithCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_collectionName == value) ? this : new Builder(this) { _collectionName = value }.Build();
        }

        public RenameCollectionOperation WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_databaseName == value) ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public RenameCollectionOperation WithDropTarget(bool? value)
        {
            return (_dropTarget == value) ? this : new Builder(this) { _dropTarget = value }.Build();
        }

        public RenameCollectionOperation WithNewCollectionName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return (_newCollectionName == value) ? this : new Builder(this) { _newCollectionName = value }.Build();
        }

        public RenameCollectionOperation WithNewDatabaseName(string value)
        {
            Ensure.IsNullOrNotEmpty(value, "value");
            return (_newDatabaseName == value) ? this : new Builder(this) { _newDatabaseName = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public string _collectionName;
            public string _databaseName;
            public bool? _dropTarget;
            public string _newCollectionName;
            public string _newDatabaseName;

            // constructors
            public Builder(RenameCollectionOperation other)
            {
                _collectionName = other.CollectionName;
                _databaseName = other.DatabaseName;
                _dropTarget = other.DropTarget;
                _newCollectionName = other.NewCollectionName;
                _newDatabaseName = other.NewDatabaseName;
            }

            // methods
            public RenameCollectionOperation Build()
            {
                return new RenameCollectionOperation(
                    _collectionName,
                    _databaseName,
                    _dropTarget,
                    _newCollectionName,
                    _newDatabaseName);
            }
        }
    }
}
