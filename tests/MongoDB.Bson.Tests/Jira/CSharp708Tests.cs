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

using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira.CSharp708
{
    public class CSharp708Tests
    {
        interface IIdentity
        {
            string Id { get; }
        }

        class Entity : IIdentity
        {
            public string Id { get; set; }
        }

        BsonMemberMap GetIdMemberMap<T>(BsonClassMap<T> cm)
            where T : class, IIdentity, new()
        {
            return cm.GetMemberMap(x => x.Id);
        }

        [Fact]
        public void TestGetMemberFindsCorrectMember()
        {
            var classMap = new BsonClassMap<Entity>();
            classMap.AutoMap();

            var memberMap = GetIdMemberMap<Entity>(classMap);

            Assert.NotNull(memberMap);
            Assert.Equal("Id", memberMap.MemberName);
        }
    }
}