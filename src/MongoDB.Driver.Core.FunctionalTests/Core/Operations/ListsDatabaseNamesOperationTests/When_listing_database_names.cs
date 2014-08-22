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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.ListDatabaseNamesOperationTests
{
    [TestFixture]
    public class When_listing_database_names : CollectionUsingSpecification
    {
        private ListDatabaseNamesOperation _subject;
        private IReadOnlyList<string> _result;

        protected override void Given()
        {
            _subject = new ListDatabaseNamesOperation();

            // make sure there is at least 1 database...
            Insert(new [] { new BsonDocument("x", 1) });
        }

        protected override void When()
        {
            _result = ExecuteOperation(_subject);
        }

        [Test]
        public void It_should_return_all_the_names()
        {
            _result.Count.Should().BeGreaterThan(0);
            _result.Should().Contain(_databaseName);
        }
    }
}
