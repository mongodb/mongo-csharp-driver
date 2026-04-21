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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5928Tests : LinqIntegrationTest<CSharp5928Tests.ClassFixture>
{
    public CSharp5928Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Non_translatable_expression_should_throw_expected_error()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(c => c.CustomerId.CompareTo(Regex.Replace("AROUT", "OUT".ToUpper(), c.CustomerId, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5))) > 0);

        var exception = Record.Exception(() => queryable.ToList());

        exception.Should().BeOfType<ExpressionNotSupportedException>().Subject
            .Message.Should().Contain("Replace(\"AROUT\", \"OUT\", c.CustomerId, IgnoreCase, 00:00:05)");
    }

    public class C
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData => null;
    }
}
