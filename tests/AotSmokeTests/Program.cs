/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace AotSmokeTests
{
    public class Person
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class Holder<T>
    {
        public T Value { get; set; }
    }

    public static class Program
    {
        private static int __failures;

        public static int Main()
        {
            var uri = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";
            var workarounds = Environment.GetEnvironmentVariable("AOT_SMOKE_WORKAROUNDS") == "1";
            Console.WriteLine($"Runtime: {Environment.Version}, AOT: {!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}, workarounds: {workarounds}");

            if (workarounds)
            {
                // App-side registrations that sidestep known AOT failures so later steps can be exercised.
                // These document what currently breaks; they are not a fix.
                Step("register workaround serializers", RegisterWorkaroundSerializers);
            }

            // Server-less round trips through a POCO, one per member type, so each failure is isolated.
            RoundTripStep("POCO with List<string> member", new List<string> { "a", "b" }, v => v.Count == 2);
            RoundTripStep("POCO with string[] member", new[] { "c" }, v => v.Length == 1);
            RoundTripStep("POCO with Dictionary<string, int> member", new Dictionary<string, int> { ["k"] = 1 }, v => v["k"] == 1);
            RoundTripStep("POCO with int? member", (int?)5, v => v == 5);

            IMongoCollection<Person> collection = null;
            Step("connect", () =>
            {
                var settings = MongoClientSettings.FromConnectionString(uri);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
                var client = new MongoClient(settings);
                var database = client.GetDatabase("aot_smoke");
                database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                database.DropCollection("people");
                collection = database.GetCollection<Person>("people");
            });
            if (collection == null)
            {
                return 1;
            }

            Step("insert", () =>
            {
                var people = new[]
                {
                    new Person { Name = "Alice", Age = 30 },
                    new Person { Name = "Bob", Age = 40 },
                    new Person { Name = "Carol", Age = 50 }
                };
                collection.InsertMany(people);
                Check(collection.CountDocuments(FilterDefinition<Person>.Empty) == 3, "expected 3 documents");
                Check(people.All(p => p.Id != ObjectId.Empty), "expected the driver to assign Person.Id client-side");
            });

            Step("raw read (BsonDocument)", () =>
            {
                var raw = collection.Database.GetCollection<BsonDocument>("people").Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefault();
                Console.WriteLine($"  stored document: {raw}");
                var classMap = BsonClassMap.LookupClassMap(typeof(Person));
                Console.WriteLine($"  class map: id member = {classMap.IdMemberMap?.MemberName ?? "<none>"}, members = [{string.Join(", ", classMap.AllMemberMaps.Select(m => m.MemberName + "->" + m.ElementName))}]");
                Check(classMap.IdMemberMap?.MemberName == "Id", "expected Person.Id to be mapped as the id member");
            });

            Step("find (filter builder)", () =>
            {
                var found = collection.Find(Builders<Person>.Filter.Eq(x => x.Name, "Bob")).ToList();
                Check(found.Count == 1 && found[0].Age == 40, "expected Bob aged 40");
            });

            Step("update", () =>
            {
                var result = collection.UpdateOne(
                    Builders<Person>.Filter.Eq(x => x.Name, "Alice"),
                    Builders<Person>.Update.Set(x => x.Age, 31));
                Check(result.ModifiedCount == 1, "expected 1 modified document");
            });

            Step("aggregate", () =>
            {
                var results = collection.Aggregate()
                    .Match(Builders<Person>.Filter.Gte(x => x.Age, 31))
                    .Group(new BsonDocument { { "_id", BsonNull.Value }, { "total", new BsonDocument("$sum", "$Age") } })
                    .ToList();
                Check(results.Count == 1 && results[0]["total"].ToInt32() == 121, "expected total age 121");
            });

            Step("LINQ Where", () =>
            {
                var n = 35;
                var results = collection.AsQueryable().Where(x => x.Age > n).ToList();
                Check(results.Count == 2, "expected 2 people older than 35");
            });

            Console.WriteLine(__failures == 0 ? "ALL PASSED" : $"{__failures} STEP(S) FAILED");
            return __failures == 0 ? 0 : 1;
        }

        private static void RegisterWorkaroundSerializers()
        {
            // ExpandoObjectSerializer's parameterless ctor is trimmed away because the provider creates it via reflection.
            BsonSerializer.RegisterSerializer(new ExpandoObjectSerializer());
            // Same root cause for primitive serializers created by PrimitiveSerializationProvider.
            BsonSerializer.RegisterSerializer(new StringSerializer());
            BsonSerializer.RegisterSerializer(new Int32Serializer());
            BsonSerializer.RegisterSerializer(new Int64Serializer());
            BsonSerializer.RegisterSerializer(new DoubleSerializer());
            BsonSerializer.RegisterSerializer(new BooleanSerializer());
            BsonSerializer.RegisterSerializer(new ObjectIdSerializer());
            // Roots generic instantiations that the LINQ serializer finder creates with MakeGenericType in a static ctor.
            GC.KeepAlive(new NullableSerializer<BsonBinarySubType>(new EnumSerializer<BsonBinarySubType>()));
        }

        private static void RoundTripStep<T>(string name, T value, Func<T, bool> verify)
        {
            Step(name, () =>
            {
                var bytes = new Holder<T> { Value = value }.ToBson();
                var result = BsonSerializer.Deserialize<Holder<T>>(bytes);
                Check(verify(result.Value), "value did not round trip");
            });
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion failed: " + message);
            }
        }

        private static void Step(string name, Action action)
        {
            try
            {
                action();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception ex)
            {
                __failures++;
                Console.WriteLine($"FAIL {name}: {ex.GetType().FullName}: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
