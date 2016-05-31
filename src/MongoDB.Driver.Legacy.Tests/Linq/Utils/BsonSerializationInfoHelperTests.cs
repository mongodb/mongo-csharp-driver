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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Builders;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Utils
{
    public class BsonSerializationInfoHelperTests
    {
        private class Test
        {
            [BsonElement("p")]
            public int Primitive { get; set; }

            [BsonElement("c")]
            public Test2 Complex { get; set; }

            [BsonElement("pe")]
            public List<int> PrimitiveEnumerable { get; set; }

            [BsonElement("ce")]
            public List<Test2> ComplexEnumerable { get; set; }
        }

        private class Test2
        {
            [BsonElement("p")]
            public int Primitive { get; set; }

            [BsonElement("c")]
            public Test3 Complex { get; set; }

            [BsonElement("pe")]
            public List<int> PrimitiveEnumerable { get; set; }

            [BsonElement("ce")]
            public List<Test3> ComplexEnumerable { get; set; }
        }

        private class Test3
        {
            [BsonElement("p")]
            public int Primitive { get; set; }
        }

        private class Test4 : Test3
        {
            [BsonElement("pe")]
            public List<int> PrimitiveEnumerable { get; set; }
        }

        private class Test5 : Test
        {
            [BsonElement("s")]
            public string String { get; set; }
        }

        [Fact]
        public void TestPrimitiveMember()
        {
            var query = Query<Test>.EQ(t => t.Primitive, 1);

            Assert.Equal("{ \"p\" : 1 }", query.ToString());
        }

        [Fact]
        public void TestComplexMember()
        {
            var query = Query<Test>.EQ(t => t.Complex, null);

            Assert.Equal("{ \"c\" : null }", query.ToString());
        }

        [Fact]
        public void TestComplexMemberMember()
        {
            var query = Query<Test>.EQ(t => t.Complex.Primitive, 1);

            Assert.Equal("{ \"c.p\" : 1 }", query.ToString());
        }

        [Fact]
        public void TestComplexMemberComplex()
        {
            var query = Query<Test>.EQ(t => t.Complex.Complex, null);

            Assert.Equal("{ \"c.c\" : null }", query.ToString());
        }

        [Fact]
        public void TestComplexMemberComplexMember()
        {
            var query = Query<Test>.EQ(t => t.Complex.Complex.Primitive, 1);

            Assert.Equal("{ \"c.c.p\" : 1 }", query.ToString());
        }

        [Fact]
        public void TestPrimitiveEnumerable()
        {
            var query = Query<Test>.EQ(t => t.PrimitiveEnumerable, null);

            Assert.Equal("{ \"pe\" : null }", query.ToString());
        }

        [Fact]
        public void TestComplexEnumerable()
        {
            var query = Query<Test>.EQ(t => t.ComplexEnumerable, null);

            Assert.Equal("{ \"ce\" : null }", query.ToString());
        }

        [Fact]
        public void TestPrimitiveIndex()
        {
            var query = Query<Test>.EQ(t => t.PrimitiveEnumerable[0], 1);

            Assert.Equal("{ \"pe.0\" : 1 }", query.ToString());
        }

        [Fact]
        public void TestComplexIndex()
        {
            var query = Query<Test>.EQ(t => t.ComplexEnumerable[1], null);

            Assert.Equal("{ \"ce.1\" : null }", query.ToString());
        }

        [Fact]
        public void TestComplexIndexMember()
        {
            var query = Query<Test>.EQ(t => t.ComplexEnumerable[1].Primitive, 2);

            Assert.Equal("{ \"ce.1.p\" : 2 }", query.ToString());
        }

        [Fact]
        public void TestPrimitiveElementAt()
        {
            var query = Query<Test>.EQ(t => t.PrimitiveEnumerable.ElementAt(0), 1);

            Assert.Equal("{ \"pe.0\" : 1 }", query.ToString());
        }

        [Fact]
        public void TestComplexElementAt()
        {
            var query = Query<Test>.EQ(t => t.ComplexEnumerable.ElementAt(0), null);

            Assert.Equal("{ \"ce.0\" : null }", query.ToString());
        }

        [Fact]
        public void TestComplexElementAtMember()
        {
            var query = Query<Test>.EQ(t => t.ComplexEnumerable.ElementAt(0).Primitive, 1);

            Assert.Equal("{ \"ce.0.p\" : 1 }", query.ToString());
        }

        [Fact]
        public void TestRootConversion()
        {
            var query = Query<Test>.EQ(t => ((Test5)t).String, "f");

            Assert.Equal("{ \"s\" : \"f\" }", query.ToString());
        }

        [Fact]
        public void TestNestedConversion()
        {
            var query = Query<Test>.EQ(t => ((Test4)t.Complex.Complex).PrimitiveEnumerable, null);
            
            Assert.Equal("{ \"c.c.pe\" : null }", query.ToString());
        }
    }
}