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

using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.MongoDatabaseImplTests
{
    [TestFixture]
    public class When_running_a_command_with_json : SpecificationBase
    {
        private string _command;
        private BsonDocument _result;

        protected override void Given()
        {
            _command = "{ ismaster: 1 }";
        }

        protected override void When()
        {
            _result = _database.RunCommandAsync<BsonDocument>(_command).Result;
        }

        [Test]
        public void the_result_should_be_ok()
        {
            _result["ok"].ToBoolean().Should().BeTrue();
        }
    }
}