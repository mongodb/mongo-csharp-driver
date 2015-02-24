using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public abstract class TranslatorTestBase
    {
        protected IMongoCollection<Root> _collection;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            var client = DriverTestConfiguration.Client;
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            _collection = db.GetCollection<Root>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            db.DropCollectionAsync(_collection.CollectionNamespace.CollectionName).GetAwaiter().GetResult();

            var root = new Root
            {
                A = "Awesome",
                B = "Balloon",
                C = new C
                {
                    D = "Dexter",
                    E = new E
                    {
                        F = 11,
                        H = 22,
                        I = new[] { "it", "icky" }
                    }
                },
                G = new[] { 
                        new C
                        {
                            D = "Don't",
                            E = new E
                            {
                                F = 33,
                                H = 44,
                                I = new [] { "ignanimous"}
                            }
                        },
                        new C
                        {
                            D = "Dolphin",
                            E = new E
                            {
                                F = 55,
                                H = 66,
                                I = new [] { "insecure"}
                            }
                        }
                },
                Id = 10,
                J = new DateTime(2012, 12, 1, 13, 14, 15, 16, DateTimeKind.Utc),
                K = true,
                L = new HashSet<int>(new[] { 1, 3, 5 }),
                M = new[] { 2, 4, 5 },
                O = new List<long> { 10, 20, 30 }
            };
            _collection.InsertOneAsync(root).GetAwaiter().GetResult();
        }

        public class RootView
        {
            public RootView()
            {
            }

            public RootView(string property)
            {
                Property = property;
            }

            public string Property { get; set; }

            public string Field = null;
        }

        public class Root
        {
            public int Id { get; set; }

            public string A { get; set; }

            public string B { get; set; }

            public C C { get; set; }

            public IEnumerable<C> G { get; set; }

            public DateTime J { get; set; }

            public bool K { get; set; }

            public HashSet<int> L { get; set; }

            public int[] M { get; set; }

            public List<long> O { get; set; }
        }

        public class C
        {
            public string D { get; set; }

            public E E { get; set; }
        }

        public class E
        {
            public int F { get; set; }

            public int H { get; set; }

            public IEnumerable<string> I { get; set; }
        }
    }
}
