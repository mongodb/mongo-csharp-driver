/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp958Tests
    {
        private interface IPerson { }

        private class Person : IPerson
        {
            public ObjectId Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void TestAssignIdWorksWithInterface()
        {
            var collection = LegacyTestConfiguration.GetCollection<IPerson>();
            collection.Drop();

            IPerson person = new Person { Name = "Jack" };
            collection.Insert(person);
            Assert.NotEqual(ObjectId.Empty, ((Person)person).Id);
        }
    }
}