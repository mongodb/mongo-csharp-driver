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

namespace MongoDB.Bson.Tests.Serialization
{
    ////[TestFixture]
    //public class StructSerializerTests {
    //    private struct S {
    //        public int I { get; set; }
    //        public string P { get; set; }
    //    }

    //    [Fact]
    //    public void TestSAsObject() {
    //        object s = new S { I = 1, P = "x" };
    //        var json = s.ToJson<object>();
    //        var expected = "{ '_t' : 'S', 'I' : 1, 'P' : 'x' }".Replace("'", "\"");
    //        Assert.Equal(expected, json);

    //        var bson = s.ToBson<object>();
    //        var rehydrated = BsonSerializer.Deserialize<object>(bson);
    //        Assert.True(bson.SequenceEqual(rehydrated.ToBson<object>()));
    //    }

    //    [Fact]
    //    public void TestSAsS() {
    //        S s = new S { I = 1, P = "x" };
    //        var json = s.ToJson<S>();
    //        var expected = "{ 'I' : 1, 'P' : 'x' }".Replace("'", "\"");
    //        Assert.Equal(expected, json);

    //        var bson = s.ToBson<S>();
    //        var rehydrated = BsonSerializer.Deserialize<S>(bson);
    //        Assert.True(bson.SequenceEqual(rehydrated.ToBson<S>()));
    //    }
    //}
}
