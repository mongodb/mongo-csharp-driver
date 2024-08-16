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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4339Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Documents_should_work()
        {
            RequireServer.Check().Supports(Feature.DocumentsStage);
            var database = GetDatabase();
            var documents = new[] { new C { X = 1 }, new C { X = 2 }, new C { X = 3 } };

            var queryable = database
                .AsQueryable()
                .Documents(documents);

            var stages = Translate(database, queryable);
            AssertStages(
                stages,
                "{ $documents : [{ X : 1 }, { X : 2 }, { X : 3 }] }");

            var results = queryable.ToList();
            results.Select(x => x.X).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Documents_should_throw_when_source_is_null()
        {
            var source = (IQueryable<NoPipelineInput>)null;
            var documents = new C[0];

            var exception = Record.Exception(() => source.Documents(documents));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("source");
        }

        [Fact]
        public void Documents_should_throw_when_documents_is_null()
        {
            var database = GetDatabase();
            var source = database.AsQueryable();
            var documents = (C[])null;

            var exception = Record.Exception(() => source.Documents(documents));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("documents");
        }

        [Fact]
        public void Documents_with_documentSerializer_should_work()
        {
            RequireServer.Check().Supports(Feature.DocumentsStage);
            var database = GetDatabase();
            var documents = new[] { new C { X = 1 }, new C { X = 2 }, new C { X = 3 } };
            var documentSerializer = new CSerializer();

            var queryable = database
                .AsQueryable()
                .Documents(documents, documentSerializer);

            var stages = Translate(database, queryable);
            AssertStages(
                stages,
                "{ $documents : [{ X : '1' }, { X : '2' }, { X : '3' }] }");

            var results = queryable.ToList();
            results.Select(x => x.X).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Documents_with_documentSerializer_should_throw_when_source_is_null()
        {
            var source = (IQueryable<NoPipelineInput>)null;
            var documents = new C[0];
            var documentSerializer = BsonSerializer.LookupSerializer<C>();

            var exception = Record.Exception(() => source.Documents(documents, documentSerializer));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("source");
        }

        [Fact]
        public void Documents_with_documentSerializer_should_throw_when_documents_is_null()
        {
            var database = GetDatabase();
            var source = database.AsQueryable();
            var documents = (C[])null;
            var documentSerializer = BsonSerializer.LookupSerializer<C>();

            var exception = Record.Exception(() => source.Documents(documents, documentSerializer));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("documents");
        }

        [Fact]
        public void Documents_with_documentSerializer_should_throw_when_documentSerializer_is_null()
        {
            var database = GetDatabase();
            var source = database.AsQueryable();
            var documents = new C[0];
            var documentSerializer = (IBsonSerializer<C>)null;

            var exception = Record.Exception(() => source.Documents(documents, documentSerializer));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("documentSerializer");
        }

        public class C
        {
            public int X { get; set; }
        }

        public class CSerializer : ClassSerializerBase<C>
        {
            public override C Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                var reader = context.Reader;
                reader.ReadStartDocument();
                reader.ReadName("X");
                var x = int.Parse(reader.ReadString());
                reader.ReadEndDocument();

                return new C { X = x };
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, C value)
            {
                var writer = context.Writer;
                writer.WriteStartDocument();
                writer.WriteName("X");
                writer.WriteString(value.X.ToString());
                writer.WriteEndDocument();
            }
        }
    }
}
