/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp595
{
    public class CSharp595Tests
    {
        [Fact]
        public void TestDoesNotThrowStackOverflowExceptionWhenConvertingToSelfType()
        {
            var id1 = new BsonObjectId(ObjectId.GenerateNewId());
            var id2 = (BsonObjectId)((IConvertible)id1).ToType(typeof(BsonObjectId), null);

            Assert.Equal(id1, id2);
        }

        [Fact]
        public void TestDoesNotThrowStackOverflowExceptionWhenConvertingToBsonString()
        {
            var id1 = new BsonObjectId(ObjectId.GenerateNewId());
            var id2 = (BsonString)((IConvertible)id1).ToType(typeof(BsonString), null);

            Assert.Equal(id1.ToString(), id2.AsString);
        }
    }
}