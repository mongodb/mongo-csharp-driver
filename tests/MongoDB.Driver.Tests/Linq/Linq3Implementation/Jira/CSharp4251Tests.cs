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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp4251Tests : LinqIntegrationTest<CSharp4251Tests.ClassFixture>
{
    public CSharp4251Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Test1()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(n =>
                new MappedObject { Items = n.Items.Select(k => new KeyValuedObject() { Count = k.Value.Count, Description = k.Value.Description, Name = k.Key }).ToList() }
            );

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { Items : { $map : { input : { $objectToArray : '$Items' }, as : 'k', in : { Count : '$$k.v.Count', Description : '$$k.v.Description', Name : '$$k.k'  } } }, _id : 0 } }");
    }

    public class DatabaseObject
    {
        public int Id { get; set; }
        public Dictionary<string, DatabaseValueObject> Items { get; set; } = new Dictionary<string, DatabaseValueObject>();

    }

    public class DatabaseValueObject
    {
        public string Description { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MappedObject
    {
        public List<KeyValuedObject> Items { get; set; } = new List<KeyValuedObject>();
    }

    public class KeyValuedObject
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public sealed class ClassFixture : MongoCollectionFixture<DatabaseObject>
    {
        protected override IEnumerable<DatabaseObject> InitialData => null;
        // [
        //     new DatabaseObject { }
        // ];
    }
}
