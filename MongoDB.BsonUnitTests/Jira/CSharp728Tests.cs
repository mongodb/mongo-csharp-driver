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
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira.CSharp728
{
    [TestFixture]
    public class CSharp728Tests
    {
        private class A
        {
            public string S { get; set; }
        }

        [Test]
        public void TestConventionProfileStillUsesDefaults()
        {
#pragma warning disable 618 
            var conventions = new ConventionProfile();
            conventions.SetElementNameConvention(new CamelCaseElementNameConvention());
            BsonClassMap.RegisterConventions(conventions, t => t == typeof(A));
#pragma warning restore 618
            var classMap = new BsonClassMap<A>();
            classMap.AutoMap();

            var memberMap = classMap.GetMemberMap(x => x.S);

            Assert.IsNotNull(memberMap);
        }
    }
}