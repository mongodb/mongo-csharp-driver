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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class RetryableUpdateCommandOperationTests : OperationTestBase
    {
        [Theory]
        [ParameterAttributeData]
        public void Let_get_and_set_should_work([Values(null, "{ name : 'name' }")] string let)
        {
            var requests = new List<UpdateRequest>
            {
                new UpdateRequest(
                    UpdateType.Update,
                    new BsonDocument("x", 1),
                    new BsonDocument("$set", new BsonDocument("x", 2)))
                {
                    Hint = new BsonDocument("_id", 1)
                }
            };
            var batch = new BatchableSource<UpdateRequest>(requests);
            var subject = new RetryableUpdateCommandOperation(_collectionNamespace, batch, _messageEncoderSettings);
            var value = let != null ? BsonDocument.Parse(let) : null;

            subject.Let = value;
            var result = subject.Let;

            result.Should().Be(value);
        }
    }
}
