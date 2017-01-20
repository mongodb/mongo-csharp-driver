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

using System.Linq;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp613
{
    public class CSharp613Tests
    {
        private class C
        {
            public int Id;
            public short S;
        }

        [Fact(Skip = "LINQ Convert")]
        public void TestShortToIntImplicitConversion()
        {
            var collection = LegacyTestConfiguration.Collection;

            collection.Drop();
            collection.Save(new C { Id = 0, S = 2 });

            var query = from c in collection.AsQueryable<C>()
                        where c.S == 2
                        select c;

            var result = query.FirstOrDefault();
            Assert.NotNull(result);
            Assert.Equal(2, result.S);
        }
    }
}