/* Copyright 2020-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class RetryableDeleteCommandOperationTests : OperationTestBase
    {
        [Theory]
        [ParameterAttributeData]
        public void Execute_with_hint_should_throw_when_hint_is_not_supported(
            [Values(0, 1)] int w,
            [Values(false, true)] bool async)
        {
            var writeConcern = new WriteConcern(w);
            var serverVersion = CoreTestConfiguration.ServerVersion;
            var requests = new List<DeleteRequest>
            {
                new DeleteRequest(new BsonDocument("x", 1))
                {
                    Hint = new BsonDocument("_id", 1)
                }
            };
            var batch = new BatchableSource<DeleteRequest>(requests);
            var subject = new RetryableDeleteCommandOperation(_collectionNamespace, batch, _messageEncoderSettings)
            {
                WriteConcern = writeConcern
            };

            var exception = Record.Exception(() => ExecuteOperation(subject, async, useImplicitSession: true));

            if (!writeConcern.IsAcknowledged)
            {
                exception.Should().BeOfType<NotSupportedException>();
            }
            else if (Feature.HintForDeleteOperations.DriverMustThrowIfNotSupported(serverVersion))
            {
                exception.Should().BeOfType<NotSupportedException>();
            }
            else if (Feature.HintForDeleteOperations.IsSupported(serverVersion))
            {
                exception.Should().BeNull();
            }
            else
            {
                exception.Should().BeOfType<MongoCommandException>();
            }
        }
    }
}
