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
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary;
using MongoDB.MongoDBClient;

namespace MongoDB.MongoDBClientTest {
    public static class Program {
        public static void Main(string[] args) {
            // test connection string pointing to server
            {
                string connectionString = "mongodb://localhost";
                var server = new MongoServer(connectionString);
                var database = server.GetDatabase("test");
                var collection = database.GetCollection<BsonDocument>("library");
                foreach (var document in collection.FindAll().Skip(0).Limit(2)) {
                    Console.WriteLine(document.ToString());
                }
            }

            // test connection string pointing to database
            {
                string connectionString = "mongodb://localhost/test";
                var database = new MongoDatabase(connectionString);
                var collection = database.GetCollection<BsonDocument>("library");
                foreach (var document in collection.FindAll().Skip(0).Limit(2)) {
                    Console.WriteLine(document.ToString());
                }
            }

            // test connection string pointing to database with default credentials
            {
                string connectionString = "mongodb://john:secret@localhost/authtest";
                var database = new MongoDatabase(connectionString);
                var collection = database.GetCollection<BsonDocument>("library");
                foreach (var document in collection.FindAll().Skip(0).Limit(2)) {
                    Console.WriteLine(document.ToString());
                }
            }
        }
    }
}
