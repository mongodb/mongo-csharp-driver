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
using MongoDB.Bson;
using MongoDB.Driver.Tests;
using Xunit;

namespace MongoDB.Driver.Legacy.Tests.Jira
{
    public class CSharp3980Tests
    {
        [Fact]
        public void AggregateExplain_should_work()
        {
            var collection = GetCollection();

            var args = new AggregateArgs
            {
                Pipeline = new[] { "{ $match : { X : 1 } }" }.Select(s => BsonDocument.Parse(s))
            };
            var result = collection.AggregateExplain(args);

            result.Ok.Should().BeTrue();
            result.Response.Should().NotBeNull();
        }

        [Fact]
        public void Cursor_Explain_should_throw()
        {
            var collection = GetCollection();
            var query = new QueryDocument("X", 1);
            var cursor = collection.Find(query);

            var exception = Record.Exception(() => cursor.Explain());

            exception.Should().BeOfType<NotSupportedException>();
        }

        private MongoCollection<C> GetCollection()
        {
            var client = DriverTestConfiguration.Client;
#pragma warning disable CS0618 // Type or member is obsolete
            var server = client.GetServer();
#pragma warning restore CS0618 // Type or member is obsolete
            var database = server.GetDatabase("test");
            return database.GetCollection<C>("test");
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
