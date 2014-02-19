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
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp595
{
    [TestFixture]
    public class CSharp595Tests
    {
        [Test]
        public void TestDoesNotThrowStackOverflowExceptionWhenConvertingToSelfType()
        {
            BsonObjectId id1 = new BsonObjectId(ObjectId.GenerateNewId());
            BsonObjectId id2 = null;
            Assert.DoesNotThrow(() =>
            {
                id2 = (BsonObjectId)((IConvertible)id1).ToType(typeof(BsonObjectId), null);
            });

            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void TestDoesNotThrowStackOverflowExceptionWhenConvertingToBsonString()
        {
            BsonObjectId id1 = new BsonObjectId(ObjectId.GenerateNewId());
            BsonString id2 = null;
            Assert.DoesNotThrow(() =>
            {
                id2 = (BsonString)((IConvertible)id1).ToType(typeof(BsonString), null);
            });

            Assert.AreEqual(id1.ToString(), id2.AsString);
        }
    }
}