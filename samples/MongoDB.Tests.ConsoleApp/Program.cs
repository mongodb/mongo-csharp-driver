using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.Tests.ConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Test1().GetAwaiter().GetResult();
            Test2().GetAwaiter().GetResult();
            Console.Read();
        }

        public static async Task Test1()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("foo");
            var collection = database.GetCollection<BsonDocument>("bar");

            await collection.InsertOneAsync(new BsonDocument("Name", "Jack"));

            var list = await collection.Find(new BsonDocument("Name", "Jack"))
                .ToListAsync();

            foreach (var document in list)
            {
                Console.WriteLine(document["Name"]);
            }
        }

        public class Person
        {
            public ObjectId Id { get; set; }
            public string Name { get; set; }
        }

        public static async Task Test2()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("foo");
            var collection = database.GetCollection<Person>("bar");

            await collection.InsertOneAsync(new Person { Name = "Jack" });

            var list = await collection.Find(x => x.Name == "Jack")
                .ToListAsync();

            foreach (var person in list)
            {
                Console.WriteLine(person.Name);
            }
        }
    }
}
