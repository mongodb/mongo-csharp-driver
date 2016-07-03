using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace MongoDB.Tests.WebApplication.Controllers
{
    public class HomeController : Controller
    {

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
            IMongoCollection<Person> collection = null;
            try
            {
                collection = database.GetCollection<Person>("bar");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            await collection.InsertOneAsync(new Person { Name = "Jack" });

            var list = await collection.Find(x => x.Name == "Jack")
                .ToListAsync();

            foreach (var person in list)
            {
                Console.WriteLine(person.Name);
            }
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            Test1().GetAwaiter().GetResult();
            Test2().GetAwaiter().GetResult();

            return View();
        }
    }
}
