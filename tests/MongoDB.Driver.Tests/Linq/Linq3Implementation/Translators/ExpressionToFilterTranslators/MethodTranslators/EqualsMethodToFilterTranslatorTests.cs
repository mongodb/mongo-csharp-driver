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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.MethodTranslators;

public class EqualsMethodToFilterTranslatorTests
{
    [Fact]
    public void Equal_should_not_throw_when_comparing_uint64_and_int32()
    {
        var client =
            new MongoClient("mongodb://localhost:27017"); // todo kyra q: is there a reason for this specific number?
        var database = client.GetDatabase("test");
        database.DropCollection("employees");

        var collection = database.GetCollection<Employee>("employees");
        collection.InsertOne(new Employee { EmployeeID = 1, ReportsTo = 2 });

        ulong longPrm = 2;

        var results = collection.AsQueryable()
            .Where(e => e.ReportsTo.Equals(longPrm))
            .ToList();

        results.Should().HaveCount(1);
    }

    public class Employee
    {
        [BsonId]
        public int EmployeeID { get; set; }
        public int? ReportsTo { get; set; }
    }
}
