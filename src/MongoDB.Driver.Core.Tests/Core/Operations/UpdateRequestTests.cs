/* Copyright 2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class UpdateRequestTests : OperationTestBase
    {
        [Fact]
        public void Constructor_should_throw_when_filter_is_null()
        {
            Action act = () => new UpdateRequest(UpdateType.Update, null, new BsonDocument());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_update_is_null()
        {
            Action act = () => new UpdateRequest(UpdateType.Update, new BsonDocument(), null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_update_is_empty_and_update_type_is_Update()
        {
            Action act = () => new UpdateRequest(UpdateType.Update, new BsonDocument(), new BsonDocument());

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_not_throw_when_update_is_empty_and_update_type_is_not_Update()
        {
            Action act = () => new UpdateRequest(UpdateType.Replacement, new BsonDocument(), new BsonDocument());

            act.ShouldNotThrow<ArgumentException>();
        }
    }
}
