/* Copyright 2010 10gen Inc.
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
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;

namespace MongoDB.BsonUnitTests.ObjectModel {
    [TestFixture]
    public class BsonValueConversionTests {
        [Test]
        public void TestFromDoubleToInt32() {
            var source = BsonDouble.Create(42);
            var target = (int)source;
        }

        [Test]
        public void TestFromDoubleToInt64() {
            var source = BsonDouble.Create(42);
            var target = (long)source;
        }

        [Test]
        public void TestFromInt32ToDouble() {
            var source = BsonInt32.Create(42);
            var target = (double)source;
        }

        [Test]
        public void TestFromInt32ToInt64()
        {
            var source = BsonInt32.Create(42);
            var target = (double)source;
        }

        [Test]
        public void TestFromInt64ToDouble() {
            var source = BsonInt64.Create(42);
            var target = (double)source;
        }

        [Test]
        public void TestFromInt64ToInt32() {
            var source = BsonInt64.Create(42);
            var target = (double)source;
        }
    }
}