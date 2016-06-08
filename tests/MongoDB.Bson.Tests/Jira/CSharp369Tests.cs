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
using System.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp369Tests
    {
        [BsonIgnoreExtraElements]
        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private class D : C
        {
            public int Y { get; set; }
        }

        [BsonIgnoreExtraElements(Inherited = true)]
        private class E
        {
            public int Id { get; set; }
            public int X { get; set; }
        }

        private class F : E
        {
            public int Y { get; set; }
        }

        [Fact]
        public void TestCWithExtraFields()
        {
            var json = "{ _id : 1, X : 2, Y : 3, Z : 4 }";
            var c = BsonSerializer.Deserialize<C>(json);
            Assert.IsType<C>(c);
            Assert.Equal(1, c.Id);
            Assert.Equal(2, c.X);
        }

        [Fact]
        public void TestDWithExtraFields()
        {
            var json = "{ _id : 1, X : 2, Y : 3, Z : 4 }";
            Assert.Throws<FormatException>(() => BsonSerializer.Deserialize<D>(json));
        }

        [Fact]
        public void TestEWithExtraFields()
        {
            var json = "{ _id : 1, X : 2, Y : 3, Z : 4 }";
            var e = BsonSerializer.Deserialize<E>(json);
            Assert.IsType<E>(e);
            Assert.Equal(1, e.Id);
            Assert.Equal(2, e.X);
        }

        [Fact]
        public void TestFWithExtraFields()
        {
            var json = "{ _id : 1, X : 2, Y : 3, Z : 4 }";
            var f = BsonSerializer.Deserialize<F>(json);
            Assert.IsType<F>(f);
            Assert.Equal(1, f.Id);
            Assert.Equal(2, f.X);
            Assert.Equal(3, f.Y);
        }
    }
}
