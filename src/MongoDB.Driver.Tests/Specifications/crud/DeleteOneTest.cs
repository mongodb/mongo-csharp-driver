﻿/* Copyright 2010-2014 MongoDB Inc.
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

using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class DeleteOneTest : CrudOperationWithResultTestBase<DeleteResult>
    {
        private BsonDocument _filter;

        protected override bool TrySetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "filter":
                    _filter = (BsonDocument)value;
                    return true;
            }

            return false;
        }

        protected override DeleteResult ConvertExpectedResult(BsonValue expectedResult)
        {
            return new DeleteResult.Acknowledged(expectedResult["deletedCount"].ToInt64());

        }
        protected override Task<DeleteResult> ExecuteAndGetResultAsync(IMongoCollection<BsonDocument> collection)
        {
            return collection.DeleteOneAsync(_filter);
        }

        protected override void VerifyResult(DeleteResult actualResult, DeleteResult expectedResult)
        {
            actualResult.DeletedCount.Should().Be(expectedResult.DeletedCount);
        }
    }
}
