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
using Xunit;

namespace MongoDB.Driver.GridFS.Tests
{
    public class GridFSMD5ExceptionTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new GridFSMD5Exception(123);

            result.Message.Should().Contain("id 123");
        }

        [Fact]
        public void constructor_should_throw_when_id_is_null()
        {
            Action action = () => new GridFSMD5Exception(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }
    }
}
