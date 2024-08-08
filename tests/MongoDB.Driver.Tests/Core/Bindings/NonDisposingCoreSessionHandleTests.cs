/* Copyright 2018-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Bindings
{
    public class NonDisposingCoreSessionHandleTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var wrapped = Mock.Of<ICoreSession>();

            var result = new NonDisposingCoreSessionHandle(wrapped);

            result._ownsWrapped().Should().BeFalse();
            result._wrapped().Should().BeSameAs(wrapped);
        }

        [Fact]
        public void Fork_should_return_expected_result()
        {
            var wrapped = Mock.Of<ICoreSession>();
            var subject = CreateSubject(wrapped: wrapped);

            var result = (NonDisposingCoreSessionHandle)subject.Fork();

            result.Should().NotBeSameAs(subject);
            result._wrapped().Should().BeSameAs(subject._wrapped());
        }

        [Fact]
        public void Dispose_should_not_dispose_wrapped()
        {
            var wrapped = Mock.Of<ICoreSession>();
            var subject = CreateSubject(wrapped: wrapped);

            subject.Dispose();

            Mock.Get(wrapped).Verify(m => m.Dispose(), Times.Never);
        }

        // private methods
        private NonDisposingCoreSessionHandle CreateSubject(
            ICoreSession wrapped)
        {
            return new NonDisposingCoreSessionHandle(wrapped);
        }
    }

    internal static class NonDisposingCoreSessionHandleReflector
    {
        public static ICoreSession _wrapped(this NonDisposingCoreSessionHandle obj) => (ICoreSession)Reflector.GetFieldValue(obj, nameof(_wrapped));
    }
}
