/* Copyright 2015 MongoDB Inc.
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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class EnumerableInterfaceImplementerSerializerTests
    {
        public class C : IEnumerable<C>
        {
            public int Id;
            public List<C> Children;

            public IEnumerator<C> GetEnumerator()
            {
                return Children.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Fact]
        public void LookupSerializer_should_not_throw_StackOverflowException()
        {
            var serializer = BsonSerializer.LookupSerializer<C>();

            serializer.Should().BeOfType<EnumerableInterfaceImplementerSerializer<C, C>>();
            var itemSerializer = ((EnumerableInterfaceImplementerSerializer<C, C>)serializer).ItemSerializer;
            itemSerializer.Should().BeSameAs(serializer);
        }

        [Fact]
        public void Serialize_should_return_expected_result()
        {
            var subject = CreateSubject();

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                var context = BsonSerializationContext.CreateRoot(jsonWriter);
                var value = new C { Id = 1, Children = new List<C> { new C { Id = 2, Children = new List<C>() } } };

                subject.Serialize(context, value);

                var json = stringWriter.ToString();
                json.Should().Be("[[]]");
            }
        }

        private IBsonSerializer<C> CreateSubject()
        {
            // create subject without using the global serializer registry
            var serializerRegistry = new BsonSerializerRegistry();
            var subject = new EnumerableInterfaceImplementerSerializer<C, C>(serializerRegistry);
            serializerRegistry.RegisterSerializer(typeof(C), subject);
            return subject;
        }
    }
}
