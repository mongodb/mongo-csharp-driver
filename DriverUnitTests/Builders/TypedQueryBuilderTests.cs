#region

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using NUnit.Framework;

#endregion

namespace MongoDB.DriverUnitTests.Builders
{
    [TestFixture]
    internal class TypeQueryBuilderTests
    {
        private class Foo
        {
            public string Bar { get; set; }
            public string Bar1 { get; set; }
            public BazClass Baz { get; set; }
        }

        private class BazClass
        {
            public string Baz1 { get; set; }
        }

        private static void AssertEquals(BsonDocument doc1, BsonDocument doc2)
        {
            if (doc1.CompareTo(doc2) != 0)
            {
                Assert.AreEqual(doc1.ToJson(), doc2.ToJson());
            }
        }

        [Test]
        public void PartialUpdate_Constant()
        {
            var ub = TypedQueryBuilder.Update(() => new Foo
                                                        {
                                                            Bar = "3"
                                                        });

            AssertEquals(ub.ToBsonDocument(),
                         new BsonDocument {{"$set", new BsonDocument {{"Bar", "3"}}}});
        }

        [Test]
        public void PartialUpdate_ConstantInSubclass()
        {
            var ub = TypedQueryBuilder.Update(() => new Foo
                                                        {
                                                            Bar = "2",
                                                            Baz = new BazClass
                                                                      {
                                                                          Baz1 = "3"
                                                                      }
                                                        });

            AssertEquals(ub.ToBsonDocument(),
                         new BsonDocument {{"$set", new BsonDocument {{"Bar", "2"}, {"Baz.Baz1", "3"}}}});
        }

        [Test]
        public void PartialUpdate_Variable()
        {
            string s = "3";
            var ub = TypedQueryBuilder.Update(() => new Foo
                                                        {
                                                            Bar = s
                                                        });

            AssertEquals(ub.ToBsonDocument(),
                         new BsonDocument {{"$set", new BsonDocument {{"Bar", "3"}}}});
        }

        [Test]
        public void Where_ConstValueInOtherClass()
        {
            var baz = new BazClass
                          {
                              Baz1 = "3"
                          };
            var ub = TypedQueryBuilder.Where((Foo f) => f.Baz.Baz1 == baz.Baz1);

            AssertEquals(ub.ToBsonDocument(),
                         Query.EQ("Baz.Baz1", "3")
                             .ToBsonDocument());
        }

        [Test]
        public void Where_Constant()
        {
            var ub = TypedQueryBuilder.Where((Foo f) => f.Bar == "3");

            AssertEquals(ub.ToBsonDocument(), Query.EQ("Bar", "3").ToBsonDocument());
        }

        [Test]
        public void Where_MultipleConditions()
        {
            var ub = TypedQueryBuilder.Where((Foo f) => f.Bar == "3" && f.Bar1 == "4");

            AssertEquals(ub.ToBsonDocument(),
                         Query.And(
                             Query.EQ("Bar", "3"),
                             Query.EQ("Bar1", "4"))
                             .ToBsonDocument());
        }

        [Test]
        public void Where_NestedMemberAccess()
        {
            var ub = TypedQueryBuilder.Where((Foo f) => f.Baz.Baz1 == "3");

            AssertEquals(ub.ToBsonDocument(),
                         Query.EQ("Baz.Baz1", "3")
                             .ToBsonDocument());
        }

        [Test]
        public void Where_NestedMemberAccessMultipleConditions()
        {
            var ub = TypedQueryBuilder.Where((Foo f) => f.Baz.Baz1 == "3" && f.Bar1 == "4");

            AssertEquals(ub.ToBsonDocument(),
                         Query.And(
                             Query.EQ("Baz.Baz1", "3"),
                             Query.EQ("Bar1", "4"))
                             .ToBsonDocument());
        }

        [Test]
        public void Where_ValueInArray()
        {
            var values = new[] {"foo", "bar", "baz"}.AsEnumerable();
            var ub = TypedQueryBuilder.Where((Foo f) => values.Contains(f.Bar));

            AssertEquals(ub.ToBsonDocument(),
                         Query.In("Bar", values.Select(BsonValue.Create).ToArray())
                             .ToBsonDocument());
        }
    }
}