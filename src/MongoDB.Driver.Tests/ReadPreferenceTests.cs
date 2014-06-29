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

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class ReadPreferenceTests
    {
        [Test]
        public void TestConstructor()
        {
            var subject = new ReadPreference();
            Assert.AreEqual(subject.ReadPreferenceMode, ReadPreferenceMode.Primary);
            Assert.IsNull(subject.TagSets);
        }

        [Test]
        public void TestCopyConstructor()
        {
            var other = new ReadPreference(ReadPreferenceMode.Nearest);
            var subject = new ReadPreference(other);

            Assert.AreEqual(subject, other);
        }

        [Test]
        public void TestGetHashCodeIsSameWhenEverythingIsTheSame()
        {
            var tagSets1 = new List<ReplicaSetTagSet>()
            {
                new ReplicaSetTagSet
                {
                    new ReplicaSetTag("dc", "ny")
                }
            };
            var readPreference1 = new ReadPreference(ReadPreferenceMode.Nearest, tagSets1);

            var tagSets2 = new List<ReplicaSetTagSet>()
            {
                new ReplicaSetTagSet
                {
                    new ReplicaSetTag("dc", "ny")
                }
            };
            var readPreference2 = new ReadPreference(ReadPreferenceMode.Nearest, tagSets2);

            Assert.AreEqual(readPreference1.GetHashCode(), readPreference2.GetHashCode());
        }

        [Test]
        public void TestEquality()
        {
            var tagSets1 = new List<ReplicaSetTagSet>()
            {
                new ReplicaSetTagSet
                {
                    new ReplicaSetTag("dc", "ny")
                }
            };
            var readPreference1 = new ReadPreference(ReadPreferenceMode.Nearest, tagSets1);

            var tagSets2 = new List<ReplicaSetTagSet>()
            {
                new ReplicaSetTagSet
                {
                    new ReplicaSetTag("dc", "ny")
                }
            };
            var readPreference2 = new ReadPreference(ReadPreferenceMode.Nearest, tagSets1);

            Assert.AreEqual(readPreference1, readPreference2);
        }

        [Test]
        public void TestAreNotEqualWhenTagSetsAreDifferent()
        {
            var tagSets1 = new List<ReplicaSetTagSet>()
            {
                new ReplicaSetTagSet
                {
                    new ReplicaSetTag("dc", "ny")
                }
            };
            var readPreference1 = new ReadPreference(ReadPreferenceMode.Nearest, tagSets1);

            var tagSets2 = new List<ReplicaSetTagSet>()
            {
                new ReplicaSetTagSet
                {
                    new ReplicaSetTag("dc", "ny")
                }
            };
            var readPreference2 = new ReadPreference(ReadPreferenceMode.Nearest, tagSets1);

            Assert.AreEqual(readPreference1, readPreference2);
        }
    }
}