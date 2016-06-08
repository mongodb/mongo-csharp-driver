/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp140
{
    public class CSharp140Tests
    {
        private class C
        {
            public int X;
        }

        [Fact]
        public void TestSerializeAnonymousClass()
        {
            var a = new { X = 1 };
            var json = a.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeCommandWrapped()
        {
            var c = new C { X = 1 };
            var w = CommandWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeFieldsWrapped()
        {
            var c = new C { X = 1 };
            var w = FieldsWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

#pragma warning disable 618
        [Fact]
        public void TestSerializeGeoNearOptionsWrapped()
        {
            var c = new C { X = 1 };
            var w = GeoNearOptionsWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }
#pragma warning restore

        [Fact]
        public void TestSerializeGroupByWrapped()
        {
            var c = new C { X = 1 };
            var w = GroupByWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeIndexKeysWrapped()
        {
            var c = new C { X = 1 };
            var w = IndexKeysWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeIndexOptionsWrapped()
        {
            var c = new C { X = 1 };
            var w = IndexOptionsWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeQueryWrapped()
        {
            var c = new C { X = 1 };
            var w = QueryWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeScopeWrapped()
        {
            var c = new C { X = 1 };
            var w = ScopeWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeSortByWrapped()
        {
            var c = new C { X = 1 };
            var w = SortByWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeUpdateWrapped()
        {
            var c = new C { X = 1 };
            var w = UpdateWrapper.Create(c);
            var json = w.ToJson();
            var expected = "{ 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSerializeUpdateReplace()
        {
            var c = new C { X = 1 };
            var w = Update.Replace<object>(c);
            var json = w.ToJson();
            var expected = "{ '_t' : 'C', 'X' : 1 }".Replace("'", "\"");
            Assert.Equal(expected, json);
        }
    }
}
