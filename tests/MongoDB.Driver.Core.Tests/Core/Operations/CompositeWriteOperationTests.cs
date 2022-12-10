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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
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

        [Theory]
        [ParameterAttributeData]
        public async Task Enumerating_operations_should_be_stopped_when_error([Values(false, true)] bool async)
        {
            var testException = new Exception("test");

            var healthyOperation1 = CreateHealthyOperation(new BsonDocument("operation", 1));
            var faultyOperation2 = CreateFaultyOperation(testException);
            var healthyOperation3= CreateHealthyOperation(new BsonDocument("operation", 3));

            var subject = new CompositeWriteOperation<BsonDocument>((healthyOperation1.Object, IsMainOperation: false), (faultyOperation2.Object, IsMainOperation: false), (healthyOperation3.Object, IsMainOperation: true));

            var resultedException = async
                ? await Record.ExceptionAsync(() => subject.ExecuteAsync(Mock.Of<IWriteBinding>(), CancellationToken.None))
                : Record.Exception(() => subject.Execute(Mock.Of<IWriteBinding>(), CancellationToken.None));

            resultedException.Should().Be(testException);

            VeryfyOperation(healthyOperation1, true, async);
            VeryfyOperation(faultyOperation2, true, async);
            VeryfyOperation(healthyOperation3, false, async);
        }

        [Theory]
        [ParameterAttributeData]
        public void Enumerating_operations_should_return_result_of_main_operation([Values(false, true)] bool async)
        {
            var operation2Result = new BsonDocument("operation", 2);

            var operation1 = CreateHealthyOperation(new BsonDocument("operation", 1));
            var operation2 = CreateHealthyOperation(operation2Result);
            var operation3 = CreateHealthyOperation(new BsonDocument("operation", 3));

            var subject = new CompositeWriteOperation<BsonDocument>((operation1.Object, IsMainOperation: false), (operation2.Object, IsMainOperation: true), (operation3.Object, IsMainOperation: false));

            var result = async
                ? subject.ExecuteAsync(Mock.Of<IWriteBinding>(), CancellationToken.None).GetAwaiter().GetResult()
                : subject.Execute(Mock.Of<IWriteBinding>(), CancellationToken.None);

            result.Should().Be(operation2Result);

            VeryfyOperation(operation1, true, async);
            VeryfyOperation(operation2, true, async);
            VeryfyOperation(operation3, true, async);
        }

        // private methods
        private Mock<IWriteOperation<BsonDocument>> CreateFaultyOperation(Exception testException)
        {
            var mockedOperation = new Mock<IWriteOperation<BsonDocument>>();
            mockedOperation
                .Setup(c => c.Execute(It.IsAny<IWriteBinding>(), It.IsAny<CancellationToken>()))
                .Throws(testException);
            mockedOperation
                .Setup(c => c.ExecuteAsync(It.IsAny<IWriteBinding>(), It.IsAny<CancellationToken>()))
                .Throws(testException);
            return mockedOperation;
        }

        private Mock<IWriteOperation<BsonDocument>> CreateHealthyOperation(BsonDocument response)
        {
            var mockedOperation = new Mock<IWriteOperation<BsonDocument>>();
            mockedOperation
                .Setup(c => c.Execute(It.IsAny<IWriteBinding>(), It.IsAny<CancellationToken>()))
                .Returns(response);
            mockedOperation
                .Setup(c => c.ExecuteAsync(It.IsAny<IWriteBinding>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            return mockedOperation;
        }

        private void VeryfyOperation(Mock<IWriteOperation<BsonDocument>> mockedOperation, bool hasBeenCalled, bool async)
        {
            if (async)
            {
                mockedOperation.Verify(c => c.ExecuteAsync(It.IsAny<IWriteBinding>(), It.IsAny<CancellationToken>()), hasBeenCalled ? Times.Once : Times.Never);
            }
            else
            {
                mockedOperation.Verify(c => c.Execute(It.IsAny<IWriteBinding>(), It.IsAny<CancellationToken>()), hasBeenCalled ? Times.Once : Times.Never);
            }
        }
    }

    internal static class CompositeWriteOperationReflector
    {
        public static (IWriteOperation<TResult> Operation, bool IsMainOperation)[] _operations<TResult>(this CompositeWriteOperation<TResult> compositeWriteOperation) => ((IWriteOperation<TResult> Operation, bool IsMainOperation)[])Reflector.GetFieldValue(compositeWriteOperation, nameof(_operations));
    }
}
