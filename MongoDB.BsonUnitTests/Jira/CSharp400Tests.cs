/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp400Tests
    {
        private static bool __testAlreadyRan = false;
        public class B
        {
            public ObjectId Id;
            public int b;
        }

        public class C : B
        {
            public int c;
        }

        [Test]
        public void TestSetIdMemberErrorMessage()
        {
            // test can only be run once
            if (__testAlreadyRan)
            {
                return;
            }
            else
            {
                __testAlreadyRan = true;
            }

            var bcm = BsonClassMap.RegisterClassMap<B>();
            var idMemberMap = bcm.GetMemberMap(b => b.Id);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                BsonClassMap.RegisterClassMap<C>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIdMember(idMemberMap); // wrong: idMemberMap is in class B, not class C
                });
            });
            var expectedMessage = "The memberMap argument must be for class C, but was for class B.";
            Assert.IsTrue(ex.Message.StartsWith(expectedMessage));
        }
    }
}
