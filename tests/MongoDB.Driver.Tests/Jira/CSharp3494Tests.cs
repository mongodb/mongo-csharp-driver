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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3494Tests : Linq3IntegrationTest
    {
        abstract class BaseDocument;

        class DerivedDocument<T> : BaseDocument
        {
            [BsonId]
            public int Id { get; set; }

            public T Value { get; set; }
        }

        [Fact]
        public void Correct_discriminator_should_be_used_for_generic_type()
        {
            var document = new DerivedDocument<int> { Id = 1, Value = 42 };
            var serialized = document.ToJson(typeof(BaseDocument));
            serialized.Should().Be("""{ "_t" : "DerivedDocument<Int32>", "_id" : 1, "Value" : 42 }""");
        }

        [BsonKnownTypes(typeof(DerivedDocument2<int>))]
        abstract class BaseDocument2 {}

        class DerivedDocument2<T> : BaseDocument2
        {
            [BsonId]
            public int Id { get; set; }

            public T Value { get; set; }
        }

        [Fact]
        public void Test2()
        {
            //This test needs to use a different set of classes than the previous one, otherwise the discriminators could have been already
            //registered, depending on the order of the tests. We need BsonKnownTypes for this to work.
            var serialized = """{ "_t" : "DerivedDocument2<Int32>", "_id" : 1, "Value" : 42 }""";
            var rehydrated = BsonSerializer.Deserialize<BaseDocument2>(serialized);
            rehydrated.Should().BeOfType<DerivedDocument2<int>>();
        }
    }
}