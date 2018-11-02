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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class KeepAliveValuesTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(ulong.MaxValue)]
        public void OnOff_get_and_set_work(ulong value)
        {
            var subject = new KeepAliveValues();

            subject.OnOff = value;
            var result = subject.OnOff;

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(ulong.MaxValue)]
        public void KeepAliveTime_get_and_set_work(ulong value)
        {
            var subject = new KeepAliveValues();

            subject.KeepAliveTime = value;
            var result = subject.KeepAliveTime;

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(ulong.MaxValue)]
        public void KeepAliveInterval_get_and_set_work(ulong value)
        {
            var subject = new KeepAliveValues();

            subject.KeepAliveInterval = value;
            var result = subject.KeepAliveInterval;

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0x0102030405060708, 0x0203040506070809, 0x030405060708090a, new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x0a, 0x09, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03 })]
        [InlineData(0xf1f2f3f4f5f6f7f8, 0xf2f3f4f5f6f7f8f9, 0xf3f4f5f6f7f8f9fa, new byte[] { 0xf8, 0xf7, 0xf6, 0xf5, 0xf4, 0xf3, 0xf2, 0xf1, 0xf9, 0xf8, 0xf7, 0xf6, 0xf5, 0xf4, 0xf3, 0xf2, 0xfa, 0xf9, 0xf8, 0xf7, 0xf6, 0xf5, 0xf4, 0xf3 })]
        public void ToBytes_should_return_expected_result(ulong onOff, ulong keepAliveTime, ulong keepAliveInterval, byte[] expectedResult)
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
