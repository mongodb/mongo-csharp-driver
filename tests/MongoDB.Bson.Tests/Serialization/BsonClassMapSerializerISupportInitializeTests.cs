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

#if !NETCOREAPP1_1
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonClassMapSerializerISupportInitializeTests
    {
        // public methods
        [Fact]
        public void BeginInit_and_EndInit_should_be_called_when_deserializing_ClassDerivedFromClassImplementingISupportInitialize()
        {
            var subject = BsonSerializer.LookupSerializer<ClassDerivedFromClassImplementingISupportInitialize>();
            using (var reader = new JsonReader("{ }"))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                var result = subject.Deserialize(context);

                result.BeginInitWasCalled.Should().BeTrue();
                result.EndInitWasCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void BeginInit_and_EndInit_should_be_called_when_deserializing_ClassDerivedFromClassImplementingISupportInitializeExplicitly()
        {
            var subject = BsonSerializer.LookupSerializer<ClassDerivedFromClassImplementingISupportInitializeExplicitly>();
            using (var reader = new JsonReader("{ }"))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                var result = subject.Deserialize(context);

                result.BeginInitWasCalled.Should().BeTrue();
                result.EndInitWasCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void BeginInit_and_EndInit_should_be_called_when_deserializing_ClassImplementingISupportInitialize()
        {
            var subject = BsonSerializer.LookupSerializer<ClassImplementingISupportInitialize>();
            using (var reader = new JsonReader("{ }"))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                var result = subject.Deserialize(context);

                result.BeginInitWasCalled.Should().BeTrue();
                result.EndInitWasCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void BeginInit_and_EndInit_should_be_called_when_deserializing_ClassImplementingISupportInitializeExplicitly()
        {
            var subject = BsonSerializer.LookupSerializer<ClassImplementingISupportInitializeExplicitly>();
            using (var reader = new JsonReader("{ }"))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                var result = subject.Deserialize(context);

                result.BeginInitWasCalled.Should().BeTrue();
                result.EndInitWasCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void BeginInit_and_EndInit_should_not_be_called_when_deserializing_ClassDerivedFromClassImplementingISupportInitializeInDifferentNamespace()
        {
            var subject = BsonSerializer.LookupSerializer<ClassDerivedFromClassImplementingISupportInitializeInDifferentNamespace>();
            using (var reader = new JsonReader("{ }"))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                var result = subject.Deserialize(context);

                result.BeginInitWasCalled.Should().BeFalse();
                result.EndInitWasCalled.Should().BeFalse();
            }
        }

        [Fact]
        public void BeginInit_and_EndInit_should_not_be_called_when_deserializing_ClassImplementingISupportInitializeInDifferentNamespace()
        {
            var subject = BsonSerializer.LookupSerializer<ClassImplementingISupportInitializeInDifferentNamespace>();
            using (var reader = new JsonReader("{ }"))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);

                var result = subject.Deserialize(context);

                result.BeginInitWasCalled.Should().BeFalse();
                result.EndInitWasCalled.Should().BeFalse();
            }
        }

        // nested classes
        public interface ISupportInitialize
        {
            void BeginInit();
            void EndInit();
        }

        public class ClassImplementingISupportInitialize : System.ComponentModel.ISupportInitialize
        {
            public bool BeginInitWasCalled { get; set; }
            public bool EndInitWasCalled { get; set; }
            public void BeginInit() { BeginInitWasCalled = true; }
            public void EndInit() { EndInitWasCalled = true; }
        }

        public class ClassImplementingISupportInitializeExplicitly : System.ComponentModel.ISupportInitialize
        {
            public bool BeginInitWasCalled { get; set; }
            public bool EndInitWasCalled { get; set; }
            void System.ComponentModel.ISupportInitialize.BeginInit() { BeginInitWasCalled = true; }
            void System.ComponentModel.ISupportInitialize.EndInit() { EndInitWasCalled = true; }
        }

        public class ClassImplementingISupportInitializeInDifferentNamespace : MongoDB.Bson.Tests.Serialization.BsonClassMapSerializerISupportInitializeTests.ISupportInitialize
        {
            public bool BeginInitWasCalled { get; set; }
            public bool EndInitWasCalled { get; set; }
            public void BeginInit() { BeginInitWasCalled = true; }
            public void EndInit() { EndInitWasCalled = true; }
        }

        public class ClassDerivedFromClassImplementingISupportInitialize : ClassImplementingISupportInitialize
        {
        }

        public class ClassDerivedFromClassImplementingISupportInitializeExplicitly : ClassImplementingISupportInitializeExplicitly
        {
        }

        public class ClassDerivedFromClassImplementingISupportInitializeInDifferentNamespace : ClassImplementingISupportInitializeInDifferentNamespace
        {
        }
    }
}
#endif
