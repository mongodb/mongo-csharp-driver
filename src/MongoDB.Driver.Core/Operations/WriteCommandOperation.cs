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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a write command operation.
    /// </summary>
    public class WriteCommandOperation : WriteCommandOperation<BsonDocument>
    {
        // constructors
        public WriteCommandOperation(string databaseName, BsonDocument command)
            : base(databaseName, command, BsonDocumentSerializer.Instance)
        {
        }
    }

    /// <summary>
    /// Represents a write command operation.
    /// </summary>
    public class WriteCommandOperation<TCommandResult> : CommandOperationBase<TCommandResult>, IWriteOperation<TCommandResult>
    {
        // constructors
        public WriteCommandOperation(string databaseName, BsonDocument command, IBsonSerializer<TCommandResult> resultSerializer)
            : base(null, command, null, databaseName, resultSerializer)
        {
        }

        private WriteCommandOperation(
            BsonDocument additionalOptions,
            BsonDocument command,
            string comment,
            string databaseName,
            IBsonSerializer<TCommandResult> resultSerializer)
            : base(additionalOptions, command, comment, databaseName, resultSerializer)
        {
        }

        // methods
        public async Task<TCommandResult> ExecuteAsync(IWriteBinding binding, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.IsNotNull(binding, "binding");
            var slidingTimeout = new SlidingTimeout(timeout);

            using (var connectionSource = await binding.GetWriteConnectionSourceAsync(slidingTimeout, cancellationToken))
            {
                return await ExecuteCommandAsync(connectionSource, ReadPreference.Primary, slidingTimeout, cancellationToken);
            }
        }

        public WriteCommandOperation<TCommandResult> WithAdditionalOptions(BsonDocument value)
        {
            return object.ReferenceEquals(AdditionalOptions, value) ? this : new Builder(this) { _additionalOptions = value }.Build();
        }

        public WriteCommandOperation<TCommandResult> WithCommand(BsonDocument value)
        {
            Ensure.IsNotNull(value, "value");
            return object.ReferenceEquals(Command, value) ? this : new Builder(this) { _command = value }.Build();
        }

        public WriteCommandOperation<TCommandResult> WithComment(string value)
        {
            return Comment == value ? this : new Builder(this) { _comment = value }.Build();
        }

        public WriteCommandOperation<TCommandResult> WithDatabaseName(string value)
        {
            Ensure.IsNotNullOrEmpty(value, "value");
            return DatabaseName == value ? this : new Builder(this) { _databaseName = value }.Build();
        }

        public WriteCommandOperation<TNewCommandResult> WithResultSerializer<TNewCommandResult>(IBsonSerializer<TNewCommandResult> value)
        {
            return new WriteCommandOperation<TNewCommandResult>(
                AdditionalOptions,
                Command,
                Comment,
                DatabaseName,
                value);
        }

        // nested types
        private struct Builder
        {
            // fields
            public BsonDocument _additionalOptions;
            public BsonDocument _command;
            public string _comment;
            public string _databaseName;
            public IBsonSerializer<TCommandResult> _resultSerializer;

            // constructors
            public Builder(WriteCommandOperation<TCommandResult> other)
            {
                _additionalOptions = other.AdditionalOptions;
                _command = other.Command;
                _comment = other.Comment;
                _databaseName = other.DatabaseName;
                _resultSerializer = other.ResultSerializer;
            }

            // methods
            public WriteCommandOperation<TCommandResult> Build()
            {
                return new WriteCommandOperation<TCommandResult>(
                    _additionalOptions,
                    _command,
                    _comment,
                    _databaseName,
                    _resultSerializer);
            }
        }
    }
}
