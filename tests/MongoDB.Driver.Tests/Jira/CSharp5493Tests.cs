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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp5493Tests
    {
        [Fact]
        private void Test()
        {
            BsonSerializer.TryRegisterSerializer(new ObjectSerializer(_ => true));
            var pack = new ConventionPack { new EnumRepresentationConvention(BsonType.String) };
            ConventionRegistry.Register(
                "enum",
                pack,
                t => true);

            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            database.DropCollection("recursiveClass");
            var collection = database.GetCollection<RecursiveClass>("recursiveClass");

            var toInsert = new RecursiveClass();
            collection.InsertOne(toInsert);
        }

        public class RecursiveClass
        {
            [BsonId]
            public ObjectId Id { get; set; }
            public TestEnum TestEnum { get; set; }
            public List<TestEnum> ListEnum { get; set; }
            public TestEnum[] ArrayEnum { get; set; }
            public Dictionary<string, TestEnum> DictEnum { get; set; }
            public RecursiveClass RecursiveProp { get; set; }
        }

        public enum TestEnum
        {
            One,
            Two,
        }
    }
}