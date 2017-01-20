/* Copyright 2010-2016 MongoDB Inc.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public abstract class IntegrationTestBase
    {
        protected static IMongoCollection<Root> __collection;
        protected static IMongoCollection<Other> __otherCollection;
        private static ConcurrentDictionary<Type, bool> __oneTimeSetupTracker = new ConcurrentDictionary<Type, bool>();

        protected IntegrationTestBase()
        {
            __oneTimeSetupTracker.GetOrAdd(GetType(), OneTimeSetup); // run OneTimeSetup once per subclass
        }

        private bool OneTimeSetup(Type type)
        {
            var client = DriverTestConfiguration.Client;
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            __collection = db.GetCollection<Root>(DriverTestConfiguration.CollectionNamespace.CollectionName);
            __otherCollection = db.GetCollection<Other>(DriverTestConfiguration.CollectionNamespace.CollectionName + "_other");
            db.DropCollection(__collection.CollectionNamespace.CollectionName);
            db.DropCollection(__collection.CollectionNamespace.CollectionName + "_other");

            InsertFirst();
            InsertSecond();
            InsertJoin();

            return true;
        }

        private void InsertFirst()
        {
            var root = new Root
            {
                A = "Awesome",
                B = "Balloon",
                C = new C
                {
                    D = "Dexter",
                    E = new V
                    {
                        F = 11,
                        H = 22,
                        I = new[] { "it", "icky" },
                        W = 1111
                    },
                    X = new List<E> { new E { F = 10 }, new V { W = 12 } }
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
                            },
                            S = new [] {
                                    new C
                                    {
                                        D = "Delilah"
                                    }
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
                O = new List<long> { 10, 20, 30 },
                Q = Q.One,
                R = new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc),
                T = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } },
                U = 1.23456571661743267789m
            };
            __collection.InsertOne(root);
        }

        private void InsertSecond()
        {
            var root = new RootDescended
            {
                A = "Amazing",
                B = "Baby",
                C = new C
                {
                    D = "Donkey Kong",
                    E = new E
                    {
                        F = 111,
                        H = 222,
                        I = new[] { "itchy" }
                    }
                },
                G = new[] {
                        new C
                        {
                            D = "Donald",
                            E = new E
                            {
                                F = 333,
                                H = 444,
                                I = new [] { "igloo" }
                            }
                        },
                        new C
                        {
                            D = "Durango",
                            E = new E
                            {
                                F = 555,
                                H = 666,
                                I = new [] { "icy" }
                            }
                        }
                },
                Id = 20,
                J = new DateTime(2011, 12, 1, 13, 14, 15, 16, DateTimeKind.Utc),
                K = false,
                L = new HashSet<int>(new[] { 2, 3, 4 }),
                M = new[] { 3, 5, 6 },
                O = new List<long> { 100, 200, 300 },
                P = 1.1,
                U = -1.234565723762724332233489m
            };
            __collection.InsertOne(root);
        }


        private void InsertJoin()
        {
            __otherCollection.InsertOne(new Other
            {
                Id = 10, // will join with first
                CEF = 111 // will join with second
            });
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

        public class DerivedRootView : RootView
        {
            public string DerivedProperty { get; set; }
        }

        public class Root : IRoot
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

            public Q Q { get; set; }

            public DateTime? R { get; set; }

            public Dictionary<string, int> T { get; set; }

            [BsonRepresentation(Bson.BsonType.Double, AllowTruncation = true)]
            public decimal U { get; set; }
        }

        public class RootDescended : Root
        {
            public double P { get; set; }
        }

        public class C
        {
            public string D { get; set; }

            public E E { get; set; }

            public IEnumerable<C> S { get; set; }

            public IEnumerable<E> X { get; set; }
        }

        public class E
        {
            public int F { get; set; }

            public int H { get; set; }

            public IEnumerable<string> I { get; set; }
        }

        public class V : E
        {
            public int W { get; set; }
        }

        public interface IRoot
        {
            int Id { get; set; }
        }

        public enum Q : byte
        {
            Zero,
            One
        }

        public class Other
        {
            public int Id { get; set; }

            public int CEF { get; set; }

            public List<OtherChild> Children { get; set; }
        }

        public class OtherChild
        {
            public int Y;
        }

        public class OtherChild2 : OtherChild
        {
            public int Z { get; set; }
        }
    }
}
