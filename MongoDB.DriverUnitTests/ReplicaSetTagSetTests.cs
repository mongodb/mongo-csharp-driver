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

using System.Collections.Generic;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class ReplicaSetTagSetTests
    {
        [Test]
        public void TestGetHashCodeIsSameWhenTagsAreTheSame()
        {
            var tagSet1 = new ReplicaSetTagSet
            {
                new ReplicaSetTag("dc", "ny")
            };

            var tagSet2 = new ReplicaSetTagSet
            {
                new ReplicaSetTag("dc", "ny")
            };

            Assert.AreEqual(tagSet1.GetHashCode(), tagSet2.GetHashCode());
        }

        [Test]
        public void TestAreEqualWhenTagsAreEqual()
        {
            var tagSet1 = new ReplicaSetTagSet
            {
                new ReplicaSetTag("dc", "ny")
            };

            var tagSet2 = new ReplicaSetTagSet
            {
                new ReplicaSetTag("dc", "ny")
            };

            Assert.AreEqual(tagSet1, tagSet2);
        }

        [Test]
        public void TestAreNotEqualWhenTagsAreNotEqual()
        {
            var tagSet1 = new ReplicaSetTagSet
            {
                new ReplicaSetTag("dc", "ny")
            };

            var tagSet2 = new ReplicaSetTagSet
            {
                new ReplicaSetTag("dc", "tx")
            };

            Assert.AreNotEqual(tagSet1, tagSet2);
        }
    }
}