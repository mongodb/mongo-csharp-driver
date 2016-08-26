/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class ImmutableTypeClassMapConventionTests
    {
        private class TestClassA
        {
            public string A { get; set; }
            public string B { get; set; }
        }

        private class TestClassB
        {
            public string A { get; }
            public string B { get; }

            public TestClassB(string a, string b)
            {
                A = a;
                B = b;
            }
        }

        private class TestClassC
        {
            public string A { get; }
            public string B { get; }

            public TestClassC(string a)
                : this(a, null)
            {
            }

            public TestClassC(string a, string b)
            {
                A = a;
                B = b;
            }
        }

        [Fact]
        public void TestNoDefaultConstructorClassMapConventionWithTestClassA()
        {
            var convention = new ImmutableTypeClassMapConvention();
            var classMap = new BsonClassMap<TestClassA>();
            convention.Apply(classMap);
            Assert.False(classMap.HasCreatorMaps);
        }

        [Fact]
        public void TestNoDefaultConstructorClassMapConventionWithTestClassB()
        {
            var convention = new ImmutableTypeClassMapConvention();
            var classMap = new BsonClassMap<TestClassB>();
            convention.Apply(classMap);
            Assert.True(classMap.HasCreatorMaps);
            Assert.Equal(1, classMap.CreatorMaps.Count());
        }

        [Fact]
        public void TestNoDefaultConstructorClassMapConventionWithTestClassC()
        {
            var convention = new ImmutableTypeClassMapConvention();
            var classMap = new BsonClassMap<TestClassC>();
            convention.Apply(classMap);
            Assert.True(classMap.HasCreatorMaps);
            Assert.Equal(1, classMap.CreatorMaps.Count());
        }
    }
}
