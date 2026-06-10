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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5929Tests : LinqIntegrationTest<CSharp5929Tests.ClassFixture>
{
    public CSharp5929Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Test1()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => x.Property[0] );

        var exception = Record.Exception(() => queryable.ToList());
        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    [BsonSerializer(typeof(CSerializer))]
    public class C
    {
        public long[] Property { get; set; }
    }

    public class CSerializer : IBsonDocumentSerializer, IBsonSerializer<C>
    {
        public Type ValueType => typeof(C);
        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => throw new NotImplementedException();
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, C value) => throw new NotImplementedException();

        C IBsonSerializer<C>.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => throw new NotImplementedException();
        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value) => throw new NotImplementedException();

        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = null;
            return false;
        }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData => null;
    }
}
