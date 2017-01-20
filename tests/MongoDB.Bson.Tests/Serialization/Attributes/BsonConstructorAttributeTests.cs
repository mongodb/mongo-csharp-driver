/* Copyright 2016 MongoDB Inc.
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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Attributes
{
    public class BsonConstructorAttributeWhenArgumentNamesProvidedTests
    {
        [Fact]
        public void constructor_with_int_should_be_mapped_correctly()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(C));

            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(int) });
            var creatorMap = classMap.CreatorMaps.Where(c => c.MemberInfo == constructorInfo).SingleOrDefault();
            creatorMap.Should().NotBeNull();
            var expectedArguments = new[]
            {
                typeof(C).GetTypeInfo().GetProperty("X")
            };
            creatorMap.Arguments.Should().Equal(expectedArguments);
        }

        [Fact]
        public void constructor_with_int_and_string_should_be_mapped_correctly()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(C));

            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(int), typeof(string) });
            var creatorMap = classMap.CreatorMaps.Where(c => c.MemberInfo == constructorInfo).SingleOrDefault();
            creatorMap.Should().NotBeNull();
            var expectedArguments = new[]
            {
                typeof(C).GetTypeInfo().GetProperty("X"),
                typeof(C).GetTypeInfo().GetProperty("Y")
            };
            creatorMap.Arguments.Should().Equal(expectedArguments);
        }

        [Fact]
        public void constructor_with_string_should_be_mapped_correctly()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(C));

            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(string) });
            var creatorMap = classMap.CreatorMaps.Where(c => c.MemberInfo == constructorInfo).SingleOrDefault();
            creatorMap.Should().NotBeNull();
            var expectedArguments = new[]
            {
                typeof(C).GetTypeInfo().GetProperty("Y")
            };
            creatorMap.Arguments.Should().Equal(expectedArguments);
        }

        // nested types
        private class C
        {
            [BsonConstructor("X")]
            public C(int a) { }

            [BsonConstructor("Y")]
            public C(string b) { }

            [BsonConstructor("X", "Y")]
            public C(int a, string b) { }

            public int X { get; private set; }
            public string Y { get; private set; }
        }
    }

    public class BsonConstructorAttributeWhenArgumentNamesNotProvidedTests
    {
        [Fact]
        public void constructor_with_int_should_be_mapped_correctly()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(C));

            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(int) });
            var creatorMap = classMap.CreatorMaps.Where(c => c.MemberInfo == constructorInfo).SingleOrDefault();
            creatorMap.Should().NotBeNull();
            var expectedArguments = new[]
            {
                typeof(C).GetTypeInfo().GetProperty("X")
            };
            creatorMap.Arguments.Should().Equal(expectedArguments);
        }

        [Fact]
        public void constructor_with_int_and_string_should_be_mapped_correctly()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(C));

            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(int), typeof(string) });
            var creatorMap = classMap.CreatorMaps.Where(c => c.MemberInfo == constructorInfo).SingleOrDefault();
            creatorMap.Should().NotBeNull();
            var expectedArguments = new[]
            {
                typeof(C).GetTypeInfo().GetProperty("X"),
                typeof(C).GetTypeInfo().GetProperty("Y")
            };
            creatorMap.Arguments.Should().Equal(expectedArguments);
        }

        [Fact]
        public void constructor_with_string_should_be_mapped_correctly()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(C));

            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(string) });
            var creatorMap = classMap.CreatorMaps.Where(c => c.MemberInfo == constructorInfo).SingleOrDefault();
            creatorMap.Should().NotBeNull();
            var expectedArguments = new[]
            {
                typeof(C).GetTypeInfo().GetProperty("Y")
            };
            creatorMap.Arguments.Should().Equal(expectedArguments);
        }

        // nested types
        private class C
        {
            [BsonConstructor]
            public C(int x) { }

            [BsonConstructor]
            public C(string y) { }

            [BsonConstructor]
            public C(int x, string y) { }

            public int X { get; private set; }
            public string Y { get; private set; }
        }
    }

    public class BsonConstructorAttributeWhenArgumentNameDoesNotMatchAnyMemberTests
    {
        [Fact]
        public void Apply_should_throw()
        {
            var exception = Record.Exception(() => BsonClassMap.LookupClassMap(typeof(C)));

            exception.Should().BeOfType<BsonSerializationException>();
        }

        // nested types
        private class C
        {
            [BsonConstructor("X")]
            public C(int y)
            {
            }

            public int Y { get; private set; }
        }
    }

    public class BsonConstructorAttributeWhenArgumentNamesNotProvidedAndConstructorParameterNameDoesNotMatchAnyMemberTests
    {
        [Fact]
        public void Apply_should_throw()
        {
            var exception = Record.Exception(() => BsonClassMap.LookupClassMap(typeof(C)));

            exception.Should().BeOfType<BsonSerializationException>();
        }

        // nested types
        private class C
        {
            [BsonConstructor]
            public C(int x)
            {
            }

            public int Y { get; private set; }
        }
    }

    public class BsonConstructorAttributeWhenArgumentNamesCountIsWrongTests
    {
        [Fact]
        public void Apply_should_throw()
        {
            var exception = Record.Exception(() => BsonClassMap.LookupClassMap(typeof(C)));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("arguments");
        }

        // nested types
        private class C
        {
            [BsonConstructor("X", "Y")]
            public C(int y)
            {
            }

            public int X { get; private set; }
            public int Y { get; private set; }
        }
    }
}
