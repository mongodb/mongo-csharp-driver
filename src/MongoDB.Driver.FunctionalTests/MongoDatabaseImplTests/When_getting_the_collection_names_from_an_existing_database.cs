/* Copyright 2013-2014 MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.MongoDatabaseImplTests
{
    [TestFixture]
    public class When_getting_the_collection_names_from_an_existing_database : SpecificationBase
    {
        private IReadOnlyList<string> _result;

        protected override void Given()
        {
            Insert(new[] { new BsonDocument("x", 1) });
        }

        protected override void When()
        {
            _result = _database.GetCollectionNamesAsync().GetAwaiter().GetResult();
        }

        [Test]
        public void The_result_should_not_be_null()
        {
            _result.Should().NotBeNull();
        }

        [Test]
        public void There_should_be_0_or_more_names()
        {
            _result.Count().Should().BeGreaterOrEqualTo(0);
        }
    }
}