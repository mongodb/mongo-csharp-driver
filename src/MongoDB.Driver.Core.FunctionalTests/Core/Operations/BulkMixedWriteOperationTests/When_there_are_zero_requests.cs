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

using System;
using FluentAssertions;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.BulkMixedWriteOperationTests
{
    [TestFixture]
    public class When_there_are_zero_requests : CollectionUsingSpecification
    {
        private InvalidOperationException _exception;
        private WriteRequest[] _requests;

        protected override void Given()
        {
            _requests = new WriteRequest[0];
        }

        protected override void When()
        {
            var subject = new BulkMixedWriteOperation(DatabaseName, CollectionName, _requests);
            _exception = Catch<InvalidOperationException>(() => ExecuteOperationAsync(subject).GetAwaiter().GetResult());
        }

        [Test]
        public void ExecuteOperationAsync_should_throw_an_InvalidOperationException()
        {
            _exception.Should().NotBeNull();
            _exception.Should().BeOfType<InvalidOperationException>();
        }
    }
}
