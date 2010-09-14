/* Copyright 2010 10gen Inc.
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
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;

using MongoDB.BsonLibrary;
using MongoDB.CSharpDriver;
using MongoDB.CSharpDriver.Builders;

namespace MongoDB.MongoDBClientTest {
    public static class Program {
        public static void Main(string[] args) {
#if false
            // test connection string pointing to server
            {
                string connectionString = "mongodb://localhost";
                var server = MongoServer.Create(connectionString);
                var database = server.GetDatabase("test");
                var collection = database.GetCollection<BsonDocument>("library");
                foreach (var document in collection.FindAll().Skip(1).Limit(1)) {
                    Console.WriteLine(document.ToJson());
                }
            }
#endif

#if false
            // test connection string pointing to database
            {
                string connectionString = "mongodb://localhost/test";
                var database = MongoDatabase.Create(connectionString);
                var collection = database.GetCollection<BsonDocument>("library");
                foreach (var document in collection.FindAll().Snapshot()) {
                    Console.WriteLine(document.ToJson());
                }
            }
#endif

#if false
            // test connection string pointing to database with default credentials
            {
                string connectionString = "mongodb://localhost/test";
                var database = MongoDatabase.Create(connectionString);
                var collection = database.GetCollection<BsonDocument>("books");
                //var fields = new BsonDocument {
                //    { "author", 1 },
                //    { "_id", 0 }
                //};
                var fields = Fields.Include("author", "title").Exclude("_id").Slice("comments", 20, 10);
                foreach (var document in collection.FindAll(fields).Skip(0).Limit(2)) {
                    Console.WriteLine(document.ToJson());
                }
            }
#endif

#if false
            var connectionString = "server=localhost;database=test";
            var server = MongoServer.Create(connectionString);
            foreach (string databaseName in server.GetDatabaseNames()) {
                Console.WriteLine(databaseName);
            }

            var database = MongoDatabase.Create(connectionString);
            foreach (string collectionName in database.GetCollectionNames()) {
                Console.WriteLine(collectionName);
            }
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection<BsonDocument>("library");
            int count;
            //count = collection.Count();
            //count = collection.Count(new BsonDocument { { "author" , "Hemmingway" } });
            //count = collection.FindAll().Count();
            //count = collection.Find(new BsonDocument { { "author" , "Hemmingway" } }).Count();
            count = collection.FindAll().Skip(2).Count();
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection<BsonDocument>("library");
            var cursor = collection.FindAll();
            var explanation = cursor.Explain(false);
            BsonJsonWriterSettings jsonSettings = new BsonJsonWriterSettings {
                Indent = true,
                IndentChars = "    "
            };
            Console.WriteLine(explanation.ToJson(jsonSettings));
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection<BsonDocument>("library");
            var document = new BsonDocument {
                { "_id", "123456789" },
                { "author" , "Tom Clancy" },
                { "title", "Inside the CIA" }
            };

            database.UseDedicatedConnection = true;
            var result = collection.Insert(document, false); // safeMode
            var lastError = database.GetLastError();
            database.ReleaseDedicatedConnection();
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var result = database.CurrentOp();
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var server = MongoServer.Create(connectionString);
            var result = server.RenameCollection("test.library", "test.books");
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection("books");
            var result = collection.Update(
                new BsonDocument("author", "Dick"), // query
                new BsonDocument("$set", new BsonDocument("author", "Harry")),
                true // safeMode
            );
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection("books");
            var result = collection.Distinct("author");
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection("books");
            var query = new BsonDocument("author", "Tom Clancy");
            var result1 = collection.Remove(query, RemoveFlags.Single, true);
            var result2 = collection.Remove(query, true);
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection("books");
            //var result = collection.FindAndModify(
            //    new BsonDocument("author", "Hemmingway Jr."), // query
            //    null, // sort
            //    new BsonDocument("$set", new BsonDocument("author", "Hemmingway Sr.")), // update
            //    true // returnNew
            //);
            var result = collection.FindAndRemove(
                new BsonDocument("author", "Hemmingway Sr."), // query
                null // sort
            );
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection("books");
            var result = collection.Group(
                new BsonDocument("author", new BsonDocument("$gt", "H")), // query
                new BsonDocument("author", 1), // keys
                new BsonDocument("total", 0), // initial
                new BsonJavaScriptWithScope(
                    "function(doc, prev) { prev.total += 1; }", // code
                    new BsonDocument("xyz", 10) // scope (btw: scope doesn't seem to work with group)
                ), // reduce
                null // finalize
            );
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection("books");
            var map = "function() { for (var key in this) { emit(key, {count: 1}); } }";
            var reduce = "function(key, emits) { total = 0; for (var i in emits) { total += emits[i].count; } return {count: total}; }";
            var mapReduceResult = collection.MapReduce(map, reduce);
            var results = mapReduceResult.GetResults<BsonDocument>().ToArray();
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection("books");
            var result = collection.Validate();
            Console.WriteLine(result.ToJson(new BsonJsonWriterSettings { Indent = true }));
#endif

#if false
            string connectionString = "mongodb://localhost,localhost/test"; // fake replica set test by using localhost twice
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection("books");
            database.Server.Connect(TimeSpan.FromMinutes(10));

            database.UseDedicatedConnection = true;
            var book = new BsonDocument {
                { "author", "Tom Jones" },
                { "title", "Life is a song" }
            };
            collection.Insert(book);
            book = collection.FindOne<BsonDocument>(book);
            database.UseDedicatedConnection = false;
#endif

#if false
            string connectionString = "mongodb://test:test@localhost/test";
            var database = MongoDatabase.Create(connectionString);
            database.RequestStart();
            var collection = database.GetCollection<BsonDocument>("books");
            var books = collection.FindAll();
            //foreach (var book in books) {
            //    Console.WriteLine(book.ToJson(new BsonJsonWriterSettings { Indent = true }));
            //}
            BsonDocument book;
            // used all these to verify that LINQ is good about calling Dispose on enumerators
            //book = collection.FindOne();
            //book = collection.FindAll().First();
            //book = collection.FindAll().FirstOrDefault();
            //var e1 = collection.FindAll().DefaultIfEmpty(null);
            //book = collection.FindAll().ElementAt(0);
            //book = collection.FindAll().Last();
            ////book = collection.FindAll().Single();
            //var array = collection.FindAll().ToArray();
            //var list = collection.FindAll().ToList();
            var count = collection.Count();
            database.RequestDone();
#endif

#if false
            string connectionString = "mongodb://test:test@localhost/test";
            var database = MongoDatabase.Create(connectionString);
            var collection = database.GetCollection<BsonDocument>("tail");
            var cursor = collection.FindAll().Sort("_id").Flags(QueryFlags.TailableCursor);
            while (true) {
                foreach (var doc in cursor) {
                    var id = doc["_id"];
                    Console.WriteLine(id);
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
#endif

#if true
            // test sending replSetInitiate to brand new replica set
            string connectionString = "mongodb://kilimanjaro:10002"; // arbitrarily chose 2nd member to send init command to
            var server = MongoServer.Create(connectionString);
            server.SlaveOk = true;

            var initCommand = new BsonDocument {
                { "replSetInitiate", new BsonDocument {
                    { "_id" , "azure" },
                    { "members", new BsonArray {
                        new BsonDocument {
                            { "_id", 1 },
                            { "host", "kilimanjaro:10001" }
                        },
                        new BsonDocument {
                            { "_id", 2 },
                            { "host", "kilimanjaro:10002" }
                        }}
                    }}
                }
            };
            var commandResult = server.RunAdminCommand(initCommand); // takes about 10 seconds to return

            server.SlaveOk = false; // forces a Disconnect becaues value is changing
            while (true) {
                try {
                    // this is going to throw exceptions until the replica set has finished electing a primary
                    int count = server["foo"]["bar"].Count(); // arbitrary trivial operation
                    break;
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            }
#endif
        }
    }
}
