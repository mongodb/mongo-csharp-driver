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

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;

namespace MongoDB.Driver.TestConsoleApplication
{
    class FilterMeasuring
    {
        public static async Task TestAsync()
        {
            var client = new MongoClient();
            var db = client.GetDatabase("test");
            var col = db.GetCollection<Person>("people");
            await col.DeleteManyAsync(x => true);

            //warm up
            for (int i = 0; i < 1000; i++)
            {
                await Builder(col);
                await ExpressionMethod(col);
                await Linq(col);
            }

            // measure
            await Measure("Builder", col, Builder);
            await Measure("Expression Method", col, ExpressionMethod);
            await Measure("Linq", col, Linq);
            Console.ReadKey();
        }

        private static async Task Measure(string name, IMongoCollection<Person> col, Func<IMongoCollection<Person>, Task> test)
        {
            const int iterations = 100000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                await test(col);
            }
            sw.Stop();
            Console.WriteLine("{0}: {1} / {2}ms = {3}/s", name, iterations, sw.ElapsedMilliseconds, ((double)iterations / sw.ElapsedMilliseconds) * 1000);
        }

        private static Task Builder(IMongoCollection<Person> col)
        {
            var builder = Builders<Person>.Filter;
            var filter = builder.Eq(x => x.Id, "my id") & builder.Eq(x => x.Name, "Jack");

            filter.Render(col.DocumentSerializer, col.Settings.SerializerRegistry);

            // await col.Find(filter).ToListAsync();
            return Task.FromResult(true);
        }

        private static Task ExpressionMethod(IMongoCollection<Person> col)
        {
            var filter = new ExpressionFilterDefinition<Person>(x => x.Id == "my id" && x.Name == "Jack");
            filter.Render(col.DocumentSerializer, col.Settings.SerializerRegistry);
            //await col.Find(x => x.Id == "my id" && x.Name == "Jack").ToListAsync();
            return Task.FromResult(true);
        }

        private static Task Linq(IMongoCollection<Person> col)
        {
            col.AsQueryable().Where(x => x.Id == "my id" && x.Name == "Jack").GetExecutionModel();
            return Task.FromResult(true);
        }

        private class Person
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}
