/* Copyright 2019-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class EstimatedDocumentCountTest : CrudOperationWithResultTestBase<long>
    {
        private readonly EstimatedDocumentCountOptions _options = new EstimatedDocumentCountOptions();

        protected override long ConvertExpectedResult(BsonValue expectedResult)
        {
            return expectedResult.ToInt64();
        }

        protected override long ExecuteAndGetResult(IMongoDatabase database, IMongoCollection<BsonDocument> collection, bool async)
        {
            if (async)
            {
#pragma warning disable 618
                return collection.EstimatedDocumentCountAsync(_options).GetAwaiter().GetResult();
#pragma warning restore
            }
            else
            {
#pragma warning disable 618
                return collection.EstimatedDocumentCount(_options);
#pragma warning restore
            }
        }

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "limit":
                    _options.MaxTime = new TimeSpan(value.ToInt64());
                    return true;
            }

            return false;
        }

        protected override void VerifyResult(long actualResult, long expectedResult)
        {
            actualResult.Should().Be(expectedResult);
        }
    }
}
