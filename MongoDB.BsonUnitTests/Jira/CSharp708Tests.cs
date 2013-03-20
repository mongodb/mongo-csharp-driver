/* Copyright 2010-2012 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.BsonUnitTests.Jira.CSharp708
{
    [TestFixture]
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

        void ConfigureClassMap<T>(BsonClassMap<T> cm)
            where T : class, IIdentity, new()
        {
            cm.SetIdMember(cm.GetMemberMap(c => c.Id).SetRepresentation(BsonType.ObjectId));
        }

        [Test]
        public void Test()
        {
            var classMap = new BsonClassMap<Entity>();
            classMap.AutoMap();

            ConfigureClassMap<Entity>(classMap);
        }
    }
}