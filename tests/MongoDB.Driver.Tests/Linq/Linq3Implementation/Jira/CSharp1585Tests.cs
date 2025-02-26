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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Optimizers;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp1585Tests : LinqIntegrationTest<CSharp1585Tests.ClassFixture>
    {
        public CSharp1585Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Filter_Builder_Where_should_translate_correctly()
        {
            var collection = Fixture.Collection;
            var filter = Builders<Document>.Filter.Where(
                document => document.Details.A.Any(x => x.Any(y => Regex.IsMatch(y.DeviceName, @".Name0."))));

            var find = collection.Find(filter);

            var translatedFilter = TranslateFindFilter(collection, find);
            translatedFilter.Should().Be("{ 'Details.A' : { $elemMatch : { $elemMatch : { DeviceName : /.Name0./ } } } }");
        }

        [Fact]
        public void Filter_Builder_ElemMatch_ElemMatch_should_translate_correctly()
        {
            var collection = Fixture.Collection;
            var deviceFilter = Builders<Device>.Filter.Regex(x => x.DeviceName, new BsonRegularExpression(".Name0."));
            var deviceArrayFilter = Builders<Device[]>.Filter.ElemMatch(deviceFilter);
            var filter = Builders<Document>.Filter.ElemMatch(x => x.Details.A, deviceArrayFilter);

            var find = collection.Find(filter);

            var translatedFilter = TranslateFindFilter(collection, find);
            translatedFilter.Should().Be("{ 'Details.A' : { $elemMatch : { $elemMatch : { DeviceName : /.Name0./ } } } }");
        }

        [Fact]
        public void Filter_Builder_ElemMatch_when_TDocument_does_not_implement_IEnumerable_should_throw()
        {
            var impliedElementFilter = Builders<int>.Filter.Where(x => x == 0);

            var exception = Record.Exception(() => Builders<Document>.Filter.ElemMatch(impliedElementFilter));

            exception.Should().BeOfType<ArgumentException>();
            exception.Message.Should().Contain("ElemMatch without a field name requires that MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira.CSharp1585Tests+Document implement IEnumerable<System.Int32>.");
        }

        [Fact]
        public void AstFilter_should_handle_nested_elemMatch()
        {
            var ast = AstFilter.ElemMatch(
                new AstFilterField("Details.A"),
                AstFilter.ElemMatch(
                    new AstFilterField("@<elem>"),
                    AstFilter.Regex(new AstFilterField("DeviceName"), ".Name0.", "")));
            var simplifiedAst = AstSimplifier.Simplify(ast);

            var rendered = simplifiedAst.Render();

            rendered.Should().Be("{ 'Details.A' : { $elemMatch : { $elemMatch : { DeviceName : /.Name0./ } } } }");
        }

        // nested types
        public class Document
        {
            public int Id { get; set; }
            public Details Details { get; set; }
        }

        public class Details
        {
            public Device[][] A;
        }

        public class Device
        {
            public string DeviceName { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Document>
        {
            protected override IEnumerable<Document> InitialData => null;
        }
    }
}
