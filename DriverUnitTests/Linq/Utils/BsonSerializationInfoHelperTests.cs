/* Copyright 2010-2012 10gen Inc.
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
using MongoDB.Driver.Linq.Utils;
using System.Linq.Expressions;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.DriverUnitTests.Linq.Utils
{
    [TestFixture]
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

        [Test]
        public void TestPrimitiveMember()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.Primitive, 1));

            Assert.AreEqual("{ \"p\" : 1 }", query.ToString());
        }

        [Test]
        public void TestComplexMember()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.Complex, null));

            Assert.AreEqual("{ \"c\" : null }", query.ToString());
        }

        [Test]
        public void TestComplexMemberMember()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.Complex.Primitive, 1));

            Assert.AreEqual("{ \"c.p\" : 1 }", query.ToString());
        }

        [Test]
        public void TestComplexMemberComplex()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.Complex.Complex, null));

            Assert.AreEqual("{ \"c.c\" : null }", query.ToString());
        }

        [Test]
        public void TestComplexMemberComplexMember()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.Complex.Complex.Primitive, 1));

            Assert.AreEqual("{ \"c.c.p\" : 1 }", query.ToString());
        }

        [Test]
        public void TestPrimitiveEnumerable()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.PrimitiveEnumerable, null));

            Assert.AreEqual("{ \"pe\" : null }", query.ToString());
        }

        [Test]
        public void TestComplexEnumerable()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.ComplexEnumerable, null));

            Assert.AreEqual("{ \"ce\" : null }", query.ToString());
        }

        [Test]
        public void TestPrimitiveIndex()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.PrimitiveEnumerable[0], 1));

            Assert.AreEqual("{ \"pe.0\" : 1 }", query.ToString());
        }

        [Test]
        public void TestComplexIndex()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.ComplexEnumerable[1], null));

            Assert.AreEqual("{ \"ce.1\" : null }", query.ToString());
        }

        [Test]
        public void TestComplexIndexMember()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.ComplexEnumerable[1].Primitive, 2));

            Assert.AreEqual("{ \"ce.1.p\" : 2 }", query.ToString());
        }

        [Test]
        public void TestPrimitiveElementAt()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.PrimitiveEnumerable.ElementAt(0), 1));

            Assert.AreEqual("{ \"pe.0\" : 1 }", query.ToString());
        }

        [Test]
        public void TestComplexElementAt()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.ComplexEnumerable.ElementAt(0), null));

            Assert.AreEqual("{ \"ce.0\" : null }", query.ToString());
        }

        [Test]
        public void TestComplexElementAtMember()
        {
            var query = Query.Build<Test>(qb => qb.EQ(t => t.ComplexEnumerable.ElementAt(0).Primitive, 1));

            Assert.AreEqual("{ \"ce.0.p\" : 1 }", query.ToString());
        }


    }
}