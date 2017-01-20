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

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp263Tests
    {
        public class C
        {
            public int Id;
            public object Obj;
        }

        [Fact]
        public void TestArrayEmpty()
        {
            var c = new C { Id = 1, Obj = new int[] { } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[]', '_v' : [] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestArrayOneElement()
        {
            var c = new C { Id = 1, Obj = new int[] { 1 } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[]', '_v' : [1] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestArrayTwoElements()
        {
            var c = new C { Id = 1, Obj = new int[] { 1, 2 } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[]', '_v' : [1, 2] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestTwoDimensionalArrayEmpty()
        {
            var c = new C { Id = 1, Obj = new int[0, 0] };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[,]', '_v' : [] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestTwoDimensionalArrayOneElement()
        {
            var c = new C { Id = 1, Obj = new int[,] { { 1, 2 } } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[,]', '_v' : [[1, 2]] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestTwoDimensionalArrayTwoElements()
        {
            var c = new C { Id = 1, Obj = new int[,] { { 1, 2 }, { 3, 4 } } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[,]', '_v' : [[1, 2], [3, 4]] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestThreeDimensionalArrayEmpty()
        {
            var c = new C { Id = 1, Obj = new int[0, 0, 0] };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[,,]', '_v' : [] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestThreeDimensionalArrayOneElement()
        {
            var c = new C { Id = 1, Obj = new int[,,] { { { 1 }, { 2 } } } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[,,]', '_v' : [[[1], [2]]] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }

        [Fact]
        public void TestThreeDimensionalArrayTwoElements()
        {
            var c = new C { Id = 1, Obj = new int[,,] { { { 1 }, { 2 } }, { { 3 }, { 4 } } } };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'Obj' : { '_t' : 'System.Int32[,,]', '_v' : [[[1], [2]], [[3], [4]]] } }".Replace("'", "\"");
            Assert.Equal(expected, json);

            var r = BsonSerializer.Deserialize<C>(json);
            Assert.Equal(c.Id, r.Id);
            Assert.Equal(c.Obj, r.Obj);
        }
    }
}
