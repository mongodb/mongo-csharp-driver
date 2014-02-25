namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    using System;
    using Bson;
    using Bson.Serialization;
    using Bson.Serialization.Conventions;
    using Bson.Serialization.Options;
    using NUnit.Framework;

    public class DecimalRepresentationConventionTests
    {
        private class TestClass
        {
            public decimal Decimal { get; set; }
            public string NotDecimal { get; set; }
        }

        [Test]
        public void TestDoesNotApplyToANonDecimalType()
        {
            var convention = new DecimalRepresentationConvention(BsonType.Double);
            var classMap = new BsonClassMap<TestClass>();
            var nonDecimalMemberMap = classMap.MapMember(x => x.NotDecimal);

            convention.Apply(nonDecimalMemberMap);

            Assert.IsNull(nonDecimalMemberMap.SerializationOptions);
        }

        [Test]
        [TestCase(BsonType.Array)]
        [TestCase(BsonType.Double)]
        [TestCase(BsonType.Int32)]
        [TestCase(BsonType.Int64)]
        [TestCase(BsonType.String)]
        public void TestChangesDecimalRepresentation(BsonType representation)
        {
            var convention = new DecimalRepresentationConvention(representation);
            var classMap = new BsonClassMap<TestClass>();
            var decimalMemberMap = classMap.MapMember(x => x.Decimal);

            convention.Apply(decimalMemberMap);

            Assert.AreEqual(representation, ((RepresentationSerializationOptions) decimalMemberMap.SerializationOptions).Representation);
        }

        [Test]
        public void TestOnlyCreateWithAllowedRepresentations()
        {
            foreach (BsonType representation in Enum.GetValues(typeof (BsonType)))
            {
                if ((representation == BsonType.Array) ||
                    (representation == BsonType.Double) ||
                    (representation == BsonType.Int32) ||
                    (representation == BsonType.Int64) ||
                    (representation == BsonType.String))
                {
                    new DecimalRepresentationConvention(representation);
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => { new DecimalRepresentationConvention(representation); });
                }
            }
        }
    }
}