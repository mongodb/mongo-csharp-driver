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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class NamedParameterCreatorMapConventionTests
    {
        [Fact]
        public void Apply_should_do_nothing_when_creator_map_already_has_arguments_configured()
        {
            var subject = new NamedParameterCreatorMapConvention();
            var classMap = new BsonClassMap<C>();
            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(int) });
            var creatorMap = classMap.MapConstructor(constructorInfo, "Y");
            var originalArguments = creatorMap.Arguments;

            subject.Apply(creatorMap);

            creatorMap.Arguments.Should().BeSameAs(originalArguments);
        }

        [Fact]
        public void Apply_should_do_nothing_when_member_info_is_null()
        {
            var subject = new NamedParameterCreatorMapConvention();
            var classMap = new BsonClassMap<C>();
            var @delegate = (Func<int, C>)(y => new C(y));
            var creatorMap = classMap.MapCreator(@delegate);
            creatorMap.Arguments.Should().BeNull();
            creatorMap.MemberInfo.Should().BeNull();

            subject.Apply(creatorMap);

            creatorMap.Arguments.Should().BeNull();
        }

        [Fact]
        public void Apply_should_do_nothing_when_constructor_parameter_name_does_not_match_any_property_or_field()
        {
            var subject = new NamedParameterCreatorMapConvention();
            var classMap = new BsonClassMap<C>();
            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(int) });
            var creatorMap = classMap.MapConstructor(constructorInfo);
            creatorMap.Arguments.Should().BeNull();

            subject.Apply(creatorMap);

            creatorMap.Arguments.Should().BeNull();
        }

        [Fact]
        public void Apply_should_set_arguments_when_constructor_parameter_names_match_a_field()
        {
            var subject = new NamedParameterCreatorMapConvention();
            var classMap = new BsonClassMap<C>();
            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(long) });
            var creatorMap = classMap.MapConstructor(constructorInfo);
            creatorMap.Arguments.Should().BeNull();

            subject.Apply(creatorMap);

            creatorMap.Arguments.Cast<FieldInfo>().Select(p => p.Name).Should().Equal(new[] { "F" });
        }

        [Fact]
        public void Apply_should_set_arguments_when_constructor_parameter_names_match_a_property()
        {
            var subject = new NamedParameterCreatorMapConvention();
            var classMap = new BsonClassMap<C>();
            var constructorInfo = typeof(C).GetTypeInfo().GetConstructor(new[] { typeof(string) });
            var creatorMap = classMap.MapConstructor(constructorInfo);
            creatorMap.Arguments.Should().BeNull();

            subject.Apply(creatorMap);

            creatorMap.Arguments.Cast<PropertyInfo>().Select(p => p.Name).Should().Equal(new[] { "P" });
        }

        // nested types
        private class C
        {
            public C(int x) { }
            public C(long f) { }
            public C(string p) { }

            public int Y { get; set; }
            public long F = 0;
            public string P { get; set; }
        }
    }
}
