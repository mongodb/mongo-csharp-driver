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
    public class GridFSDownloadByNameOptionsTests
    {
        [Fact]
        public void default_constructor_should_return_expected_result()
        {
            var result = new GridFSDownloadByNameOptions();

            result.Revision.Should().Be(-1);
        }

        [Fact]
        public void Revision_get_should_return_expected_result()
        {
            var subject = new GridFSDownloadByNameOptions { Revision = 2 };

            var result = subject.Revision;

            result.Should().Be(2);
        }

        [Fact]

        public void Revision_set_should_have_expected_result()
        {
            var subject = new GridFSDownloadByNameOptions();

            subject.Revision = 2;

            subject.Revision.Should().Be(2);
        }
    }
}
