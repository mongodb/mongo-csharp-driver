/* Copyright 2015-2016 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.GridFS.Tests
{
    public class GridFSChunkExceptionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new GridFSChunkException(123, 2, "missing");

            result.Message.Should().Contain("file id 123");
            result.Message.Should().Contain("chunk 2");
            result.Message.Should().Contain("missing");
        }

        [Fact]
        public void constructor_should_throw_when_id_is_null()
        {
            Action action = () => new GridFSChunkException(null, 2, "missing");

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }

        [Fact]
        public void constructor_should_throw_when_n_is_negative()
        {
            Action action = () => new GridFSChunkException(123, -2, "missing");

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("n");
        }

        [Fact]
        public void constructor_should_throw_when_reason_is_null()
        {
            Action action = () => new GridFSChunkException(123, 2, null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("reason");
        }
    }
}
