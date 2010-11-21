using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Linq;

using NUnit.Framework;

namespace MongoDB.LinqUnitTests
{
    [TestFixture]
    public class MapReduceTests : LinqTestsBase
    {
        public override void TestSetup()
        {
            base.TestSetup();

            Collection.RemoveAll(SafeMode.True);
            Collection.Insert(
                new Person
                {
                    FirstName = "Bob",
                    LastName = "McBob",
                    Age = 42,
                    PrimaryAddress = new Address { City = "London" },
                    Addresses = new List<Address> 
                    {
                        new Address { City = "London" },
                        new Address { City = "Tokyo" }, 
                        new Address { City = "Seattle" } 
                    },
                    EmployerIds = new[] { 1, 2 }
                }, SafeMode.True);

            Collection.Insert(
                new Person
                {
                    FirstName = "Jane",
                    LastName = "McJane",
                    Age = 35,
                    PrimaryAddress = new Address { City = "Paris" },
                    Addresses = new List<Address> 
                    {
                        new Address { City = "Paris" }
                    },
                    EmployerIds = new[] { 1 }

                }, SafeMode.True);

            Collection.Insert(
                new Person
                {
                    FirstName = "Joe",
                    LastName = "McJoe",
                    Age = 21,
                    PrimaryAddress = new Address { City = "Chicago" },
                    Addresses = new List<Address> 
                    {
                        new Address { City = "Chicago" },
                        new Address { City = "London" }
                    },
                    EmployerIds = new[] { 3 }
                }, SafeMode.True);
        }

        [Test]
        public void Off_of_select()
        {
            var minAge = Collection.Linq().Select(x => x.Age).Min();

            Assert.AreEqual(21, minAge);
        }

        [Test]
        public void Off_of_root()
        {
            var minAge = Collection.Linq().Min(x => x.Age);

            Assert.AreEqual(21, minAge);
        }

        [Test]
        public void NoGrouping()
        {
            var grouping = Enumerable.ToList(from p in Collection.Linq()
                                             where p.Age > 21
                                             group p by 1 into g
                                             select new
                                             {
                                                 Average = g.Average(x => x.Age),
                                                 Min = g.Min(x => x.Age),
                                                 Max = g.Max(x => x.Age),
                                                 Count = g.Count(),
                                                 Sum = g.Sum(x => x.Age)
                                             });

            Assert.AreEqual(1, grouping.Count);
            Assert.AreEqual(38.5, grouping.Single().Average);
            Assert.AreEqual(35, grouping.Single().Min);
            Assert.AreEqual(42, grouping.Single().Max);
            Assert.AreEqual(2, grouping.Single().Count);
            Assert.AreEqual(77, grouping.Single().Sum);
        }

        [Test]
        public void Expression_Grouping()
        {
            var grouping = Enumerable.ToList(from p in Collection.Linq()
                                             group p by p.Age % 2 into g
                                             select new
                                             {
                                                 IsEven = g.Key == 0,
                                                 Min = g.Min(x => x.Age),
                                                 Max = g.Max(x => x.Age),
                                                 Count = g.Count(),
                                                 Sum = g.Sum(x => x.Age)
                                             });

            Assert.AreEqual(2, grouping.Count);
            Assert.AreEqual(1, grouping[0].Count);
            Assert.AreEqual(42, grouping[0].Max);
            Assert.AreEqual(42, grouping[0].Min);
            Assert.AreEqual(42, grouping[0].Sum);
            Assert.AreEqual(2, grouping[1].Count);
            Assert.AreEqual(35, grouping[1].Max);
            Assert.AreEqual(21, grouping[1].Min);
            Assert.AreEqual(56, grouping[1].Sum);
        }

        [Test]
        public void Expression_Grouping2()
        {
            var grouping = Enumerable.ToList(from p in Collection.Linq()
                                             group p by p.FirstName[0] into g
                                             select new
                                             {
                                                 FirstLetter = g.Key,
                                                 Min = g.Min(x => x.Age),
                                                 Max = g.Max(x => x.Age)
                                             });

            Assert.AreEqual(2, grouping.Count);
            Assert.AreEqual('B', grouping[0].FirstLetter);
            Assert.AreEqual(42, grouping[0].Max);
            Assert.AreEqual(42, grouping[0].Min);
            Assert.AreEqual('J', grouping[1].FirstLetter);
            Assert.AreEqual(35, grouping[1].Max);
            Assert.AreEqual(21, grouping[1].Min);
        }

        [Test]
        public void Complex()
        {
            var grouping = Enumerable.ToList(from p in Collection.Linq()
                                             where p.Age > 21
                                             group p by new { FirstName = p.FirstName, LastName = p.LastName } into g
                                             select new
                                             {
                                                 Name = g.Key.FirstName + " " + g.Key.LastName,
                                                 Min = g.Min(x => x.Age) + 100,
                                                 Max = g.Max(x => x.Age) + 100
                                             });

            Assert.AreEqual(2, grouping.Count);
            Assert.AreEqual("Bob McBob", grouping[0].Name);
            Assert.AreEqual(142, grouping[0].Max);
            Assert.AreEqual(142, grouping[0].Min);
            Assert.AreEqual("Jane McJane", grouping[1].Name);
            Assert.AreEqual(135, grouping[1].Max);
            Assert.AreEqual(135, grouping[1].Min);
        }
    }
}