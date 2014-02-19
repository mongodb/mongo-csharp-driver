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

using System.Linq;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class ConventionPackTests
    {
        private ConventionPack _pack;

        [SetUp]
        public void SetUp()
        {
            _pack = new ConventionPack();
        }

        [Test]
        public void TestAdd()
        {
            _pack.Add(new TestConvention { Name = "One" });
            _pack.Add(new TestConvention { Name = "Two" });

            Assert.AreEqual(2, _pack.Conventions.Count());
        }

        [Test]
        public void TestAddClassMapConvention()
        {
            _pack.AddClassMapConvention("test", cm => { });

            Assert.IsInstanceOf<DelegateClassMapConvention>(_pack.Conventions.Single());
        }

        [Test]
        public void TestAddPostProcessingConvention()
        {
            _pack.AddPostProcessingConvention("test", cm => { });

            Assert.IsInstanceOf<DelegatePostProcessingConvention>(_pack.Conventions.Single());
        }

        [Test]
        public void TestAddMemberMapConvention()
        {
            _pack.AddMemberMapConvention("test", mm => { });

            Assert.IsInstanceOf<DelegateMemberMapConvention>(_pack.Conventions.Single());
        }

        [Test]
        public void TestAddRange()
        {
            _pack.AddRange(new IConvention[] 
            {
                new TestConvention { Name = "One" },
                new TestConvention { Name = "Two" }
            });

            Assert.AreEqual(2, _pack.Conventions.Count());
        }

        [Test]
        public void TestAppend()
        {
            _pack.AddRange(new IConvention[] 
            {
                new TestConvention { Name = "One" },
                new TestConvention { Name = "Two" }
            });

            var newPack = new ConventionPack();
            newPack.AddRange(new IConvention[] 
            {
                new TestConvention { Name = "Three" },
                new TestConvention { Name = "Four" }
            });

            _pack.Append(newPack);

            Assert.AreEqual(4, _pack.Conventions.Count());
            Assert.AreEqual("One", _pack.Conventions.ElementAt(0).Name);
            Assert.AreEqual("Two", _pack.Conventions.ElementAt(1).Name);
            Assert.AreEqual("Three", _pack.Conventions.ElementAt(2).Name);
            Assert.AreEqual("Four", _pack.Conventions.ElementAt(3).Name);
        }

        [Test]
        public void TestInsertAfter()
        {
            _pack.AddRange(new IConvention[] 
            {
                new TestConvention { Name = "One" },
                new TestConvention { Name = "Two" }
            });

            _pack.InsertAfter("One", new TestConvention { Name = "Three" });

            Assert.AreEqual(3, _pack.Conventions.Count());
            Assert.AreEqual("Three", _pack.Conventions.ElementAt(1).Name);
        }

        [Test]
        public void TestInsertBefore()
        {
            _pack.AddRange(new IConvention[] 
            {
                new TestConvention { Name = "One" },
                new TestConvention { Name = "Two" }
            });

            _pack.InsertBefore("Two", new TestConvention { Name = "Three" });
            Assert.AreEqual(3, _pack.Conventions.Count());
            Assert.AreEqual("Three", _pack.Conventions.ElementAt(1).Name);
        }

        [Test]
        public void TestRemove()
        {
            _pack.AddRange(new IConvention[] 
            {
                new TestConvention { Name = "One" },
                new TestConvention { Name = "Two" }
            });

            _pack.Remove("Two");
            Assert.AreEqual(1, _pack.Conventions.Count());
            Assert.AreEqual("One", _pack.Conventions.Single().Name);
        }

        private class TestConvention : IConvention
        {
            public string Name { get; set; }
        }
    }
}