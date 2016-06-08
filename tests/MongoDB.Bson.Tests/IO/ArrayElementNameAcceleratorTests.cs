/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class ArrayElementNameAcceleratorTests
    {
        [Theory]
        [InlineData(10)]
        public void GetElementNameBytes_should_return_expected_result(int numberOfCachedElementNames)
        {
            var subject = new ArrayElementNameAccelerator(numberOfCachedElementNames);

            for (var index = 0; index < numberOfCachedElementNames + 10; index++)
            {
                var result = subject.GetElementNameBytes(index);

                var expectedResult = Utf8Encodings.Strict.GetBytes(index.ToString());
                Assert.Equal(expectedResult, result);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void GetElementNameBytes_should_return_expected_result_for_boundary_conditions(
            [Values(0, 9, 10, 99, 100, 999, 1000, 9999, 10000, 99999, 100000, 999999, 1000000, 9999999, 100000000, int.MaxValue)]
            int index,
            [Values(0)]
            int numberOfCachedElementNames)
        {
            var subject = new ArrayElementNameAccelerator(numberOfCachedElementNames);

            var result = subject.GetElementNameBytes(index);

            var expectedResult = Utf8Encodings.Strict.GetBytes(index.ToString());
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(10)]
        public void GetElementNameBytes_should_return_new_byte_array_when_not_cached(int numberOfCachedElementNames)
        {
            var subject = new ArrayElementNameAccelerator(numberOfCachedElementNames);
            var index = numberOfCachedElementNames;

            var result1 = subject.GetElementNameBytes(index);
            var result2 = subject.GetElementNameBytes(index);

            Assert.NotSame(result1, result2);
        }

        [Theory]
        [InlineData(10)]
        public void GetElementNameBytes_should_return_same_byte_array_when_cached(int numberOfCachedElementNames)
        {
            var subject = new ArrayElementNameAccelerator(numberOfCachedElementNames);

            for (var index = 0; index < numberOfCachedElementNames; index++)
            {
                var result1 = subject.GetElementNameBytes(index);
                var result2 = subject.GetElementNameBytes(index);

                Assert.Same(result1, result2);
            }
        }

        [Fact]
        public void GetElementNameBytes_should_throw_when_index_is_negative()
        {
            var subject = new ArrayElementNameAccelerator(0);

            Assert.Throws<ArgumentOutOfRangeException>(() => subject.GetElementNameBytes(-1));
        }
    }
}
