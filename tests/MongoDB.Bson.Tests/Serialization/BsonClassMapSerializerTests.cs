/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonClassMapSerializerTests
    {
        // public methods
        [Fact]
        public void Deserialize_should_not_throw_null_ref_exception_when_class_map_creator_silently_fails()
        {
            BsonClassMap
                .LookupClassMap(typeof(MyModel))
                .SetCreator(() =>
                {
                    // here things may silently fail, especially it there's a DI container involved...

                    // simulating the silent failure:
                    return null;
                });

            var subject = BsonSerializer.LookupSerializer<MyModel>();
            using var reader = new JsonReader("{ \"_id\": \"just_an_id\" }");

            var context = BsonDeserializationContext.CreateRoot(reader);
            var exception = Record.Exception(() => subject.Deserialize(context));

            Assert.NotNull(exception);
            Assert.IsNotType<NullReferenceException>(exception);
        }

        

        // nested classes
        public class MyModel
        {
            public string Id { get; set; }
        }
    }
}
