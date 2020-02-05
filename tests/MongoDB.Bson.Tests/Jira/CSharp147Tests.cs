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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp147
{
    public class CSharp146Tests
    {
        public class Parent
        {
            public Child Child { get; set; }
        }

        public class Child
        {
            public Guid Id { get; set; }
            public int A { get; set; }
        }

        [Theory]
        [ParameterAttributeData]
        [ResetGuidModeAfterTest]
        public void Test(
            [ClassValues(typeof(GuidModeValues))] GuidMode mode)
        {
            mode.Set();

#pragma warning disable 618
            var p = new Parent { Child = new Child() };
            p.Child.A = 1;
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 && BsonDefaults.GuidRepresentation != GuidRepresentation.Unspecified)
            {
                var json = p.ToJson(new JsonWriterSettings());
                BsonSerializer.Deserialize<Parent>(json); // throws Unexpected element exception
            }
            else
            {
                var exception = Record.Exception(() => p.ToJson(new JsonWriterSettings()));
                exception.Should().BeOfType<BsonSerializationException>();
            }
#pragma warning restore 618
        }
    }
}
