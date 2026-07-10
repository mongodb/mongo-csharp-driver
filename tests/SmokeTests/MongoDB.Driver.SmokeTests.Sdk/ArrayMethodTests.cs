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
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.SmokeTests.Sdk
{
    /// <summary>
    /// We have this tests to ensure that array-extension methods works for both C# 12 and C# 14 versions.
    /// Methods tested in this class used to be compiled as LINQ methods, but with net10.0 with C# 14 they will be resolved to MemoryExtensions methods.
    /// </summary>
    [Trait("Category", "Integration")]
    public class ArrayMethodTests
    {
        [Fact]
        public void ArrayContains_should_work() =>
            RunTestCase(collection =>
            {
                var result = collection.AsQueryable()
                    .Where(d => d.Array.Contains(2))
                    .ToList();

                result.Count.Should().Be(1);
            });

        [Fact]
        public void ArraySequenceEqual_should_work() =>
            RunTestCase(collection =>
            {
                var result = collection.AsQueryable()
                    .Where(d => d.Array.SequenceEqual(new[] { 1, 2, 3 }))
                    .ToList();

                result.Count.Should().Be(1);
            });

        private void RunTestCase(Action<IMongoCollection<Model>> testCase)
        {
            var client = new MongoClient(InfrastructureUtilities.MongoUri);
            var database = client.GetDatabase("arraymethodtests_smoke_" + Guid.NewGuid().ToString("N"));

            try
            {
                var collection = database.GetCollection<Model>("test");

                collection.InsertMany(new[] { new Model { Id = 1, Array = new[] {1, 2, 3} } });

                testCase(collection);
            }
            finally
            {
                client.DropDatabase(database.DatabaseNamespace.DatabaseName);
                ClusterRegistry.Instance.UnregisterAndDisposeCluster(client.Cluster);
            }
        }

        private class Model
        {
            public int Id { get; set; }
            public int[] Array { get; set; }
        }
    }
}
