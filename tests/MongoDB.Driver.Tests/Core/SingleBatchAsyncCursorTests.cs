/* Copyright 2010-present MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver;

public class SingleBatchAsyncCursorTests
{
    [Fact]
    public async Task DisposeAsync_should_dispose_cursor()
    {
        var subject = new SingleBatchAsyncCursor<BsonDocument>(new List<BsonDocument>());

        await subject.DisposeAsync();

        Record.Exception(() => { _ = subject.Current; }).Should().BeOfType<ObjectDisposedException>();
    }

    [Fact]
    public async Task DisposeAsync_can_be_called_more_than_once()
    {
        var subject = new SingleBatchAsyncCursor<BsonDocument>(new List<BsonDocument>());

        await subject.DisposeAsync();
        var exception = await Record.ExceptionAsync(async () => await subject.DisposeAsync());

        exception.Should().BeNull();
    }
}
