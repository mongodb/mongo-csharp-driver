/* Copyright 2013-2016 MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Misc
{
    public class ReferenceCountedTests
    {
        private Mock<IDisposable> _mockDisposable;

        public ReferenceCountedTests()
        {
            _mockDisposable = new Mock<IDisposable>();
        }

        [Fact]
        public void Constructor_should_throw_if_instance_is_null()
        {
            Action act = () => new ReferenceCounted<IDisposable>(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Initial_reference_count_should_be_one()
        {
            var subject = new ReferenceCounted<IDisposable>(_mockDisposable.Object);

            subject.ReferenceCount.Should().Be(1);
        }

        [Fact]
        public void Decrement_should_not_call_dispose_when_reference_count_is_greater_than_zero()
        {
            var subject = new ReferenceCounted<IDisposable>(_mockDisposable.Object);

            subject.IncrementReferenceCount();
            subject.DecrementReferenceCount();

            subject.ReferenceCount.Should().Be(1);
            _mockDisposable.Verify(d => d.Dispose(), Times.Never);
        }

        [Fact]
        public void Decrement_should_call_dispose_when_reference_count_is_zero()
        {
            var subject = new ReferenceCounted<IDisposable>(_mockDisposable.Object);

            subject.DecrementReferenceCount();

            subject.ReferenceCount.Should().Be(0);
            _mockDisposable.Verify(d => d.Dispose(), Times.Once);
        }
    }
}
