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

using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp2480Tests
    {
        [Fact(Skip = "No need to run the test, we just need to be sure that the method calls are non-ambiguous")]
        public void Test()
        {
            var collection = new Mock<IMongoCollection<Person>>().Object;

            var replace = new Person { Name = "newName" };
            collection.FindOneAndReplace(c => c.Name == "Test", replace, new FindOneAndReplaceOptions<Person>());
            collection.FindOneAndReplaceAsync(c => c.Name == "Test", replace, new FindOneAndReplaceOptions<Person>());

            var updateDefinition = new ObjectUpdateDefinition<Person>(new Person());
            collection.FindOneAndUpdate(c => c.Name == "Test", updateDefinition, new FindOneAndUpdateOptions<Person>());
            collection.FindOneAndUpdateAsync(c => c.Name == "Test", updateDefinition, new FindOneAndUpdateOptions<Person>());

            collection.FindOneAndDelete(c => c.Name == "Test", new FindOneAndDeleteOptions<Person>());
            collection.FindOneAndDeleteAsync(c => c.Name == "Test", new FindOneAndDeleteOptions<Person>());
        }

        private class Person
        {
            public string Name { get; set; }
        }
    }
}