/* Copyright 2010-2015 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using MongoDB.Driver.Linq.Translators;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Tests.Linq.Translators
{
    public class ProjectedObjectDeserializerTests
    {
        [Fact]
        public void Should_deserialize_top_level_fields()
        {
            var result = Deserialize("{a: 1, b: 2}",
                new BsonSerializationInfo("a", new Int32Serializer(), typeof(int)),
                new BsonSerializationInfo("b", new Int32Serializer(), typeof(int)));

            result.GetValue<int>("a", null).Should().Be(1);
            result.GetValue<int>("b", null).Should().Be(2);
        }

        [Fact]
        public void Should_deserialize_unspecified_documents()
        {
            var result = Deserialize("{a: { b: 1, c: 2}}",
                new BsonSerializationInfo("a.b", new Int32Serializer(), typeof(int)),
                new BsonSerializationInfo("a.c", new Int32Serializer(), typeof(int)));

            result.GetValue<int>("a.b", null).Should().Be(1);
            result.GetValue<int>("a.c", null).Should().Be(2);
        }

        [Fact]
        public void Should_deserialize_unspecified_arrays()
        {
            var result = Deserialize("{a: [{b: 1}, {b: 2}]}",
                new BsonSerializationInfo("a.b", new Int32Serializer(), typeof(int)));

            var list = result.GetValue<IEnumerable<object>>("a", null)
                .Cast<ProjectedObject>()
                .Select(x => x.GetValue<int>("b", 10)).ToList();
            list.Should().BeEquivalentTo(1, 2);
        }

        private ProjectedObject Deserialize(string json, params BsonSerializationInfo[] serializationInfos)
        {
            using (var reader = new JsonReader(json))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                var serializer = new ProjectedObjectDeserializer(serializationInfos);
                return serializer.Deserialize(context);
            }
        }
    }
}