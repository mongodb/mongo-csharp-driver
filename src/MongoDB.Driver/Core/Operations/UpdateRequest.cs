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
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal sealed class UpdateRequest : WriteRequest
    {
        // constructors
        public UpdateRequest(UpdateType updateType, BsonDocument filter, BsonValue update)
            : base(WriteRequestType.Update)
        {
            UpdateType = updateType;
            Filter = Ensure.IsNotNull(filter, nameof(filter));
            Update = EnsureUpdateIsValid(update, updateType);
        }

        // properties
        public IEnumerable<BsonDocument> ArrayFilters { get; init; }
        public Collation Collation { get; init; }
        public BsonDocument Filter { get; init; }
        public BsonValue Hint { get; init; }
        public bool IsMulti { get; init; }
        public bool IsUpsert { get; init; }
        public BsonDocument Sort { get; init; }
        public BsonValue Update { get; init; }
        public UpdateType UpdateType { get; init; }

        // public methods
        public override bool IsRetryable(ConnectionDescription connectionDescription) => !IsMulti;

        // private methods
        private static BsonValue EnsureUpdateIsValid(BsonValue update, UpdateType updateType)
        {
            Ensure.IsNotNull(update, nameof(update));

            if (updateType == UpdateType.Update)
            {
                switch (update)
                {
                    case BsonDocument document:
                        {
                            if (document.ElementCount == 0)
                            {
                                throw new ArgumentException("Updates must have at least 1 update operator.",
                                    nameof(update));
                            }

                            break;
                        }
                    case BsonArray array:
                        {
                            if (array.Count == 0)
                            {
                                throw new ArgumentException("Updates must have at least 1 update operator in a pipeline.",
                                    nameof(update));
                            }

                            break;
                        }
                    default:
                        throw new ArgumentException("Updates must be BsonDocument or BsonArray.", nameof(update));
                }
            }

            return update;
        }
    }
}
