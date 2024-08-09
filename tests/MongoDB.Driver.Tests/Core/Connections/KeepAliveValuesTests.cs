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
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class KeepAliveValuesTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(uint.MaxValue)]
        public void OnOff_get_and_set_work(uint value)
        {
            var subject = new KeepAliveValues();

            subject.OnOff = value;
            var result = subject.OnOff;

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(uint.MaxValue)]
        public void KeepAliveTime_get_and_set_work(uint value)
        {
            var subject = new KeepAliveValues();

            subject.KeepAliveTime = value;
            var result = subject.KeepAliveTime;

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(uint.MaxValue)]
        public void KeepAliveInterval_get_and_set_work(uint value)
        {
            var subject = new KeepAliveValues();

            subject.KeepAliveInterval = value;
            var result = subject.KeepAliveInterval;

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0x01020304, 0x02030405, 0x03040506, new byte[] { 0x04, 0x03, 0x02, 0x01, 0x05, 0x04, 0x03, 0x02, 0x06, 0x05, 0x04, 0x03 })]
        [InlineData(0xf1f2f3f4, 0xf2f3f4f5, 0xf3f4f5f6, new byte[] { 0xf4, 0xf3, 0xf2, 0xf1, 0xf5, 0xf4, 0xf3, 0xf2, 0xf6, 0xf5, 0xf4, 0xf3 })]
        public void ToBytes_should_return_expected_result(uint onOff, uint keepAliveTime, uint keepAliveInterval, byte[] expectedResult)
        {
            var subject = new KeepAliveValues
            {
                OnOff = onOff,
                KeepAliveTime = keepAliveTime,
                KeepAliveInterval = keepAliveInterval
            };

            var result = subject.ToBytes();

            result.Should().Equal(expectedResult);
        }
    }
}
