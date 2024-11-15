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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class AllowedTypesConventionTests
    {
        public class C
        {
            public object ObjectProp { get; set; }
        }

        [Fact]
        public void BaseTest()
        {
            var subject = new AllowedTypesConvention();
            //Type in assembly
            subject.IsTypeAllowedForDeserialization(typeof(C)).Should().BeTrue();
            //Type not in assembly
            subject.IsTypeAllowedForDeserialization(typeof(EnumSerializer)).Should().BeFalse();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();
            //Type in assembly
            serializer.AllowedDeserializationTypes(typeof(C)).Should().BeTrue();
            //Type not in assembly
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        // private methods
        private BsonMemberMap CreateMemberMap<TMember>(Expression<Func<C, TMember>> member)
        {
            var classMap = new BsonClassMap<C>();
            return classMap.MapMember(member);
        }

    }
}