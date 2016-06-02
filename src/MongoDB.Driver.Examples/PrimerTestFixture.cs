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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MongoDB.Bson;

namespace MongoDB.Driver.Examples
{
    public abstract class PrimerTestFixture
    {
        protected static IMongoClient __client;
        protected static IMongoDatabase __database;
        private static List<BsonDocument> __dataset;
        private static Lazy<bool> __lazyOneTimeSetup = new Lazy<bool>(OneTimeSetup);
        private static bool __reloadCollection = true;

        protected PrimerTestFixture()
        {
            var _ = __lazyOneTimeSetup.Value;
            if (__reloadCollection)
            {
                LoadCollection();
                __reloadCollection = false;
            }
        }

        private static bool OneTimeSetup()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            __client = new MongoClient(connectionString);
            __database = __client.GetDatabase("test");
            LoadDataSetFromResource();
            return true;
        }

        // protected methods
        protected void AltersCollection()
        {
            __reloadCollection = true;
        }

        // helper methods
        private void LoadCollection()
        {
            __database.DropCollection("restaurants");

            var collection = __database.GetCollection<BsonDocument>("restaurants");
            collection.InsertMany(__dataset);
        }

        private static void LoadDataSetFromResource()
        {
            __dataset = new List<BsonDocument>();

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("MongoDB.Driver.Examples.dataset.json"))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var document = BsonDocument.Parse(line);
                    __dataset.Add(document);
                }
            }
        }
    }
}
