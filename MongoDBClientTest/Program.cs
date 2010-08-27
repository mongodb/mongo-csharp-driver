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

using MongoDB.BsonLibrary;
using MongoDB.MongoDBClient;

namespace MongoDB.MongoDBClientTest {
    public static class Program {
        public static void Main(string[] args) {
#if false
            // test connection string pointing to server
            {
                string connectionString = "mongodb://localhost";
                var server = MongoServer.FromConnectionString(connectionString);
                var database = server.GetDatabase("test");
                var collection = database.GetCollection<BsonDocument>("library");
                foreach (var document in collection.FindAll().Skip(1).Limit(1)) {
                    Console.WriteLine(document.ToString());
                }
            }
#endif

#if false
            // test connection string pointing to database
            {
                string connectionString = "mongodb://localhost/test";
                var database = MongoDatabase.FromConnectionString(connectionString);
                var collection = database.GetCollection<BsonDocument>("library");
                foreach (var document in collection.FindAll().Snapshot()) {
                    Console.WriteLine(document.ToString());
                }
            }
#endif

#if false
            // test connection string pointing to database with default credentials
            {
                string connectionString = "mongodb://john:secret@localhost/test";
                var database = MongoDatabase.FromConnectionString(connectionString);
                var collection = database.GetCollection<BsonDocument>("library");
                foreach (var document in collection.FindAll().Skip(0).Limit(2)) {
                    Console.WriteLine(document.ToString());
                }
            }
#endif

#if false
            var connectionString = "server=localhost;database=test";
            var server = MongoServer.FromConnectionString(connectionString);
            foreach (string databaseName in server.GetDatabaseNames()) {
                Console.WriteLine(databaseName);
            }

            var database = MongoDatabase.FromConnectionString(connectionString);
            foreach (string collectionName in database.GetCollectionNames()) {
                Console.WriteLine(collectionName);
            }
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.FromConnectionString(connectionString);
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
            var database = MongoDatabase.FromConnectionString(connectionString);
            var collection = database.GetCollection<BsonDocument>("library");
            var cursor = collection.FindAll();
            var explanation = cursor.Explain(false);
            BsonJsonWriterSettings jsonSettings = new BsonJsonWriterSettings {
                Indent = true,
                IndentChars = "    "
            };
            Console.WriteLine(explanation.ToString(jsonSettings));
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.FromConnectionString(connectionString);
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
            var database = MongoDatabase.FromConnectionString(connectionString);
            var result = database.CurrentOp();
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var server = MongoServer.FromConnectionString(connectionString);
            var result = server.RenameCollection("test.library", "test.books");
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.FromConnectionString(connectionString);
            var collection = database.GetCollection("books");
            var result = collection.Update(
                new BsonDocument("author", "Dick"), // query
                new BsonDocument("$set", new BsonDocument("author", "Harry")),
                true // safeMode
            );
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.FromConnectionString(connectionString);
            var collection = database.GetCollection("books");
            var result = collection.Distinct("author");
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.FromConnectionString(connectionString);
            var collection = database.GetCollection("books");
            var query = new BsonDocument("author", "Tom Clancy");
            var result1 = collection.Remove(query, RemoveFlags.Single, true);
            var result2 = collection.Remove(query, true);
#endif

#if false
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.FromConnectionString(connectionString);
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

#if true
            string connectionString = "mongodb://localhost/test";
            var database = MongoDatabase.FromConnectionString(connectionString);
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
        }
    }
}
