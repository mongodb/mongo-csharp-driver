/* Copyright 2010-2015 MongoDB Inc.
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

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Examples
{
    public class PrimerTestFixture
    {
        protected static IMongoClient _client;
        protected static IMongoDatabase _database;
        private static List<BsonDocument> _dataset;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase("test");

            LoadDataSetFromResource();
            LoadCollection();
        }

        [TearDown]
        public void TearDown()
        {
            var methodName = TestContext.CurrentContext.Test.Name;
            var methodInfo = GetType().GetMethod(methodName);
            var altersCollectionAttribute = methodInfo.GetCustomAttribute(typeof(AltersCollectionAttribute));
            if (altersCollectionAttribute != null)
            {
                LoadCollection();
            }
        }

        // helper methods
        private void LoadCollection()
        {
            LoadCollectionAsync().GetAwaiter().GetResult();
        }

        private async Task LoadCollectionAsync()
        {
            await _database.DropCollectionAsync("restaurants");

            var collection = _database.GetCollection<BsonDocument>("restaurants");
            await collection.InsertManyAsync(_dataset);
        }

        private void LoadDataSetFromResource()
        {
            _dataset = new List<BsonDocument>();

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("MongoDB.Driver.Examples.dataset.json"))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var document = BsonDocument.Parse(line);
                    _dataset.Add(document);
                }
            }
        }
    }
}
