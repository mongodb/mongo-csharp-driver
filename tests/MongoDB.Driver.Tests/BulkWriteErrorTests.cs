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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class BulkWriteErrorTests
    {
        [Theory]
        [InlineData(0, ServerErrorCategory.Uncategorized)]
        [InlineData(50, ServerErrorCategory.ExecutionTimeout)]
        [InlineData(11000, ServerErrorCategory.DuplicateKey)]
        [InlineData(11001, ServerErrorCategory.DuplicateKey)]
        [InlineData(12582, ServerErrorCategory.DuplicateKey)]
        public void Should_translate_category_correctly(int code, ServerErrorCategory expectedCategory)
        {
            var coreError = new Core.Operations.BulkWriteOperationError(0, code, "blah", new BsonDocument());
            var subject = BulkWriteError.FromCore(coreError);

            subject.Category.Should().Be(expectedCategory);
        }
    }
}
