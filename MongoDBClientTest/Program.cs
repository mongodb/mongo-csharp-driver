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
            using (var client = new MongoClient()) {
                var database = client["test"];
                var collection = database["library"];
                foreach (BsonDocument document in collection.Find()) {
                    Console.WriteLine(document.ToString());
                }
            }
        }
    }
}
