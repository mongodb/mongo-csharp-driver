﻿/* Copyright 2010-2011 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.BsonUnitTests.Serialization.Conventions {
    [TestFixture]
    public class ExtraElementsMemberConventionsTests {
        private class TestClassA {
            public Guid Id { get; set; }
            public BsonDocument ExtraElements { get; set; }
        }

        private class TestClassB {
            public Guid Id { get; set; }
        }

        [Test]
        public void TestNamedExtraElementsMemberConvention() {
            var convention = new NamedExtraElementsMemberConvention("ExtraElements");

            var extraElementsMemberName = convention.FindExtraElementsMember(typeof(TestClassA));
            Assert.IsNotNull(extraElementsMemberName);
            Assert.AreEqual("ExtraElements", extraElementsMemberName);

            extraElementsMemberName = convention.FindExtraElementsMember(typeof(TestClassB));
            Assert.IsNull(extraElementsMemberName);
        }
    }
}
