using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.ComponentModel;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.ComponentModel
{
    [TestFixture]
    public class ObjectIdConverterTests
    {
        [Test]
        public void TestCanGetTypeConverterForObjectId()
        {
            TypeConverter res = TypeDescriptor.GetConverter(typeof (ObjectId));
            Assert.AreEqual(typeof(ObjectIdConverter), res.GetType());
        }

        #region CanConvertxxx Tests

        [Test]
        public void TestCanConvertToString()
        {
            var converter = new ObjectIdConverter();
            Assert.IsTrue(converter.CanConvertTo(typeof (string)));
        }

        [Test]
        public void TestCanConvertToDateTime()
        {
            var converter = new ObjectIdConverter();
            Assert.IsTrue(converter.CanConvertTo(typeof (DateTime)));
        }

        [Test]
        public void TestCanConvertFromString()
        {
            var converter = new ObjectIdConverter();
            Assert.IsTrue(converter.CanConvertFrom(typeof (string)));
        }

        #endregion

        [Test]
        public void TestConvertFromObjectIdToString()
        {
            var expected = "0102030405060708090a0b0c";
            var id = new ObjectId(expected);
            var converter = new ObjectIdConverter();

            var res = converter.ConvertTo(id, typeof(string));
           
            Assert.AreEqual(typeof(string), res.GetType());
            Assert.AreEqual(expected, res);
        }

        [Test]
        public void TestConvertFromObjectIdToDateTime()
        {
            var expected = new DateTime(2011, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var id = ObjectId.GenerateNewId(expected);

            var converter = new ObjectIdConverter();

            var res = converter.ConvertTo(id, typeof(DateTime));

            Assert.AreEqual(typeof(DateTime), res.GetType());
            Assert.AreEqual(expected, res);
        }

        [Test]
        public void TestConvertFromValidStringToObjectId()
        {
            var s = "0102030405060708090a0b0c";
            var expected = new ObjectId(s);
            var converter = new ObjectIdConverter();

            var res = converter.ConvertFrom(s);

            Assert.AreEqual(typeof(ObjectId), res.GetType());
            Assert.AreEqual(expected, res);
        } 
    }
}
