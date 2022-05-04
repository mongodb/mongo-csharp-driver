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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Operations;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Operations
{
    public class CompositeWriteOperationTests
    {
        [Fact]
        public void constructor_should_initialize_operation_when_a_single_main_operation()
        {
            _ = new CompositeWriteOperation<BsonDocument>(
                (Mock.Of<IWriteOperation<BsonDocument>>(), IsMainOperation: false),
                (Mock.Of<IWriteOperation<BsonDocument>>(), IsMainOperation: true));
        }

        [Fact]
        public void constructor_should_throw_when_operations_is_null()
        {
            Record.Exception(() => new CompositeWriteOperation<BsonDocument>(operations: null)).Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("operations");
        }

        [Fact]
        public void constructor_should_throw_when_main_operations_count_is_incorrect()
        {
            Record.Exception(() => new CompositeWriteOperation<BsonDocument>()).Should().BeOfType<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("Length");

            Record.Exception(() => new CompositeWriteOperation<BsonDocument>((Mock.Of<IWriteOperation<BsonDocument>>(), IsMainOperation: false))).Should().BeOfType<ArgumentException>().Which.Message.Should().Be($"{nameof(CompositeWriteOperation<BsonDocument>)} must have a single main operation.");

            Record.Exception(() =>
                new CompositeWriteOperation<BsonDocument>(
                    (Mock.Of<IWriteOperation<BsonDocument>>(), IsMainOperation: true),
                    (Mock.Of<IWriteOperation<BsonDocument>>(), IsMainOperation: true)))
                .Should().BeOfType<ArgumentException>().Which.Message.Should().Be($"{nameof(CompositeWriteOperation<BsonDocument>)} must have a single main operation.");
        }
    }

    internal static class CompositeWriteOperationReflector
    {
        public static (IWriteOperation<TResult> Operation, bool IsMainOperation)[] _operations<TResult>(this CompositeWriteOperation<TResult> compositeWriteOperation) => ((IWriteOperation<TResult> Operation, bool IsMainOperation)[])Reflector.GetFieldValue(compositeWriteOperation, nameof(_operations));
    }
}
