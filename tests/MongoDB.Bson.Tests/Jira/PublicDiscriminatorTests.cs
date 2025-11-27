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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Jira;

public class PublicDiscriminatorTests
{
    [Fact]
    public void Deserialize_with_public_discriminator_should_work()
    {
        var json = "{ _id : 1, _t : ['C', 'D'] }";
        var serializer = BsonSerializer.LookupSerializer<C>();

        var document = BsonSerializer.Deserialize<C>(json);

        document.Should().BeOfType<D>();
        document.Id.Should().Be(1);
        document.Discriminator.Should().Equal("C", "D");
    }

    [Fact]
    public void Serialize_with_public_discriminator_should_work()
    {
        var document = new D { Id = 1 };

        var serializedDocument = document.ToBsonDocument<C>(args: new BsonSerializationArgs { SerializeIdFirst = true });

        serializedDocument.Should().Be("{ _id : 1, _t : ['C', 'D'] }");
    }

    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(D))]
    public abstract class C
    {
        public int Id { get; set; }
        [BsonDiscriminatorMember("_t")] public virtual IReadOnlyList<string> Discriminator => ["C"];
    }

    public sealed class D : C
    {
        public override IReadOnlyList<string> Discriminator => ["C", "D"];
    }
}
