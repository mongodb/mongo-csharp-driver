using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    /// <summary>
    /// Unit tests for PredicateTranslator class
    /// </summary>
    [TestFixture]
    public class PredicateTranslatorTests
    {

        private class A
        {
            public virtual ICollection<B> Bs
            { get; set; }
        }

        [BsonDiscriminator(RootClass = true)]
        private class B
        {
            public ObjectId Id;
            public int b;

            public MyEnum MyEnum
            { get; set; }
        }

        private class C : B
        {
            public int c;
        }

        private class D : C
        {
            public int d;
        }

        public enum MyEnum
        {
            Enum1 = 1,
            Enum2 = 2,
            Enum3 = 4
        }

        [Test]
        public void OfTypeMethod_should_add_discriminator()
        {
            var dValue = 1;
            Expression<Func<A, bool>> where = t => t.Bs.OfType<D>().Any(d => d.d == dValue);

            BsonSerializationInfoHelper _serializationInfoHelper = new BsonSerializationInfoHelper();
            PredicateTranslator target = new PredicateTranslator(_serializationInfoHelper);

            var actual = target.BuildQuery(where.Body);

            Assert.IsNotNull(actual);
            Assert.AreEqual("{ \"Bs\" : { \"$elemMatch\" : { \"_t\" : \"D\", \"d\" : 1 } } }", actual.ToJson());
        }

        [Test]
        public void QueryEnum()
        {
            var enumValue = MyEnum.Enum1;
            Expression<Func<B, bool>> where = t => t.MyEnum == enumValue;

            BsonSerializationInfoHelper _serializationInfoHelper = new BsonSerializationInfoHelper();
            PredicateTranslator target = new PredicateTranslator(_serializationInfoHelper);

            var actual = target.BuildQuery(where.Body);

            Assert.IsNotNull(actual);
            Assert.AreEqual("{ \"MyEnum\" : 1 }", actual.ToJson());
        }
    }
}
