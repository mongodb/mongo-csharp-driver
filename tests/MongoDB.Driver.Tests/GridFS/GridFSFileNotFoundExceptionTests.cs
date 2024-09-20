﻿/* Copyright 2015-present MongoDB Inc.
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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using MongoDB.Driver.GridFS;
using Xunit;

namespace MongoDB.Driver.Tests.GridFS
{
    public class GridFSFileNotFoundExceptionTests
    {
        [Fact]
        public void constructor_with_filename_and_revision_should_initialize_instance()
        {
            var result = new GridFSFileNotFoundException("name", 1);

            result.Message.Should().Contain("filename \"name\"");
            result.Message.Should().Contain("revision 1");
        }

        [Fact]
        public void constructor_with_filename_and_revision_should_throw_when_filename_is_null()
        {
            Action action = () => new GridFSFileNotFoundException(null, 1);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("filename");
        }

        [Fact]
        public void constructor_with_id_should_initialize_instance()
        {
            var result = new GridFSFileNotFoundException(123);

            result.Message.Should().Contain("id 123");
        }

        [Fact]
        public void constructor_with_id_should_throw_when_id_is_null()
        {
            Action action = () => new GridFSFileNotFoundException(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("id");
        }
    }
}
