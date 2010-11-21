using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Linq;
using NUnit.Framework;

namespace MongoDB.IntegrationTests.Linq
{
    [TestFixture]
    public class MongoQueryTests : LinqTestsBase
    {
        public override void TestSetup()
        {
            base.TestSetup();

            Collection.RemoveAll(SafeMode.True);
            Collection.Insert(
                new Person
                {
                    FirstName = "Bob",
                    MidName = "Bart",
                    LastName = "McBob",
                    Age = 42,
                    PrimaryAddress = new Address {City = "London", IsInternational = true, AddressType = AddressType.Company},
                    Addresses = new List<Address>
                    {
                        new Address { City = "London", IsInternational = true, AddressType = AddressType.Company },
                        new Address { City = "Tokyo", IsInternational = true, AddressType = AddressType.Private }, 
                        new Address { City = "Seattle", IsInternational = false, AddressType = AddressType.Private } 
                    },
                    EmployerIds = new[] { 1, 2 }
                }, SafeMode.True);

            Collection.Insert(
                new Person
                {
                    FirstName = "Jane",
                    LastName = "McJane",
                    Age = 35,
                    PrimaryAddress = new Address { City = "Paris", IsInternational = false, AddressType = AddressType.Private },
                    Addresses = new List<Address> 
                    {
                        new Address { City = "Paris", AddressType = AddressType.Private }
                    },
                    EmployerIds = new[] {1}
                },
                SafeMode.True);

            Collection.Insert(
                new Person
                {
                    FirstName = "Joe",
                    LastName = "McJoe",
                    Age = 21,
                    PrimaryAddress = new Address { City = "Chicago", IsInternational = true, AddressType = AddressType.Private },
                    Addresses = new List<Address> 
                    {
                        new Address { City = "Chicago", AddressType = AddressType.Private },
                        new Address { City = "London", AddressType = AddressType.Company }
                    },
                    EmployerIds = new[] {3}
                },
                SafeMode.True);
        }

        [Test]
        public void Any()
        {
            var anyone = Collection.Linq().Any(x => x.Age <= 21);

            Assert.IsTrue(anyone);
        }

        [Test]
        public void Boolean()
        {
            var people = Enumerable.ToList(Collection.Linq().Where(x => x.PrimaryAddress.IsInternational));

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void Boolean_Inverse()
        {
            var people = Enumerable.ToList(Collection.Linq().Where(x => !x.PrimaryAddress.IsInternational));

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void Boolean_In_Conjunction()
        {
            var people = Enumerable.ToList(Collection.Linq().Where(x => x.PrimaryAddress.IsInternational && x.Age > 21));

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void Chained()
        {
            var people = Collection.Linq()
                .Select(x => new { Name = x.FirstName + x.LastName, x.Age })
                .Where(x => x.Age > 21)
                .Select(x => x.Name).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void Complex_Addition()
        {
            var people = Collection.Linq().Where(x => x.Age + 23 < 50).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void Complex_Disjunction()
        {
            var people = Collection.Linq().Where(x => x.Age == 21 || x.Age == 35).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void ConjuctionConstraint()
        {
            var people = Collection.Linq().Where(p => p.Age > 21 && p.Age < 42).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void ConstraintsAgainstLocalReferenceMember()
        {
            var local = new { Test = new { Age = 21 } };
            var people = Collection.Linq().Where(p => p.Age > local.Test.Age).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void ConstraintsAgainstLocalVariable()
        {
            var age = 21;
            var people = Collection.Linq().Where(p => p.Age > age).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void Count()
        {
            var count = Collection.Linq().Count();

            Assert.AreEqual(3, count);
        }

        [Test]
        public void Count_with_predicate()
        {
            var count = Collection.Linq().Count(x => x.Age > 21);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Count_without_predicate()
        {
            var count = Collection.Linq().Where(x => x.Age > 21).Count();

            Assert.AreEqual(2, count);
        }

        [Test]
        public void DocumentQuery()
        {
            var people = (from p in DocumentCollection.Linq()
                          where p.Key("age") > 21
                          select (string)p["fn"]).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void Enum()
        {
            var people = Collection.Linq()
                .Where(x => x.PrimaryAddress.AddressType == AddressType.Company)
                .ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void First()
        {
            var person = Collection.Linq().OrderBy(x => x.Age).First();

            Assert.AreEqual("Joe", person.FirstName);
        }

        [Test]
        public void LocalEnumerable_SequenceEqual()
        {
            var ids = new[] {1, 2};
            var people = Collection.Linq().Where(x => x.EmployerIds.SequenceEqual(ids)).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void LocalEnumerable_Contains()
        {
            var names = new[] { "Joe", "Bob" };
            var people = Collection.Linq().Where(x => names.Contains(x.FirstName)).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void LocalList_Contains()
        {
            var names = new List<string> { "Joe", "Bob" };
            var people = Collection.Linq().Where(x => names.Contains(x.FirstName)).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void NestedArray_Length()
        {
            var people = (from p in Collection.Linq()
                          where p.EmployerIds.Length == 1
                          select p).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test(Description = "This will fail < 1.4")]
        public void NestedArray_indexer()
        {
            var people = Collection.Linq().Where(x => x.EmployerIds[0] == 1).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void NestedClassConstraint()
        {
            var people = Collection.Linq().Where(p => p.PrimaryAddress.City == "London").ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void NestedCollection_Count()
        {
            var people = (from p in Collection.Linq()
                          where p.Addresses.Count == 1
                          select p).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test(Description = "This will fail < 1.4")]
        public void NestedList_indexer()
        {
            var people = Collection.Linq().Where(x => x.Addresses[1].City == "Tokyo").ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void NestedQueryable_Any()
        {
            var people = Collection.Linq().Where(x => x.Addresses.Any(a => a.City == "London")).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void NestedQueryable_Contains()
        {
            var people = Collection.Linq().Where(x => x.EmployerIds.Contains(1)).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void Nested_Queryable_Count()
        {
            var people = Collection.Linq().Where(x => x.Addresses.Count() == 1).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test(Description = "This will fail < 1.4")]
        public void Nested_Queryable_ElementAt()
        {
            var people = Collection.Linq().Where(x => x.Addresses.ElementAt(1).City == "Tokyo").ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void NotNullCheck()
        {
            var people = Collection.Linq().Where(x => x.MidName != null).ToArray();

            Assert.AreEqual(1, people.Length);
        }

        [Test]
        public void NullCheck()
        {
            var people = Collection.Linq().Where(x => x.MidName == null).ToArray();

            Assert.AreEqual(2, people.Length);
        }

        [Test]
        public void NullCheckOnClassTypes()
        {
            var people = Collection.Linq().Where(x => x.LinkedId == null).ToArray();

            Assert.AreEqual(3, people.Length);
        }

        [Test]
        public void OrderBy()
        {
            var people = Collection.Linq().OrderBy(x => x.Age).ThenByDescending(x => x.LastName).ToList();

            Assert.AreEqual("Joe", people[0].FirstName);
            Assert.AreEqual("Jane", people[1].FirstName);
            Assert.AreEqual("Bob", people[2].FirstName);
        }

        [Test]
        public void Projection()
        {
            var people = (from p in Collection.Linq()
                          select new { Name = p.FirstName + p.LastName }).ToList();

            Assert.AreEqual(3, people.Count);
        }

        [Test]
        public void ProjectionWithLocalCreation_ChildobjectShouldNotBeNull()
        {
            var people = Collection.Linq()
                .Select(p => new PersonWrapper(p, p.FirstName))
                .FirstOrDefault();

            Assert.IsNotNull(people);
            Assert.IsNotNull(people.Name);
            Assert.IsNotNull(people.Person);
            Assert.IsNotNull(people.Person.PrimaryAddress);
        }

        [Test]
        public void ProjectionWithConstraints()
        {
            var people = (from p in Collection.Linq()
                          where p.Age > 21 && p.Age < 42
                          select new { Name = p.FirstName + p.LastName }).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void Regex_IsMatch()
        {
            var people = (from p in Collection.Linq()
                          where Regex.IsMatch(p.FirstName, "Joe")
                          select p).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void Regex_IsMatch_CaseInsensitive()
        {
            var people = (from p in Collection.Linq()
                          where Regex.IsMatch(p.FirstName, "joe", RegexOptions.IgnoreCase)
                          select p).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void Single()
        {
            var person = Collection.Linq().Where(x => x.Age == 21).Single();

            Assert.AreEqual("Joe", person.FirstName);
        }

        [Test]
        public void SingleEqualConstraint()
        {
            var people = Collection.Linq().Where(p => "Joe" == p.FirstName).ToList();

            Assert.AreEqual(1, people.Count);
        }

        [Test]
        public void SkipAndTake()
        {
            var people = Collection.Linq().OrderBy(x => x.Age).Skip(2).Take(1).ToList();

            Assert.AreEqual("Bob", people[0].FirstName);
        }

        [Test]
        public void String_Contains()
        {
            var people = (from p in Collection.Linq()
                          where p.FirstName.Contains("o")
                          select p).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void String_EndsWith()
        {
            var people = (from p in Collection.Linq()
                          where p.FirstName.EndsWith("e")
                          select p).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void String_StartsWith()
        {
            var people = (from p in Collection.Linq()
                          where p.FirstName.StartsWith("J")
                          select p).ToList();

            Assert.AreEqual(2, people.Count);
        }

        [Test]
        public void WithoutConstraints()
        {
            var people = Collection.Linq().ToList();

            Assert.AreEqual(3, people.Count);
        }
    }
}