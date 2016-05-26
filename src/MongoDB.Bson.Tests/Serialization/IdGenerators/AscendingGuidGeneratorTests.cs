/* Copyright 2010-2014 MongoDB Inc.
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
using System.Linq;
using MongoDB.Bson.Serialization.IdGenerators;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class AscendingGuidGeneratorTests
    {
        private AscendingGuidGenerator _generator = new AscendingGuidGenerator();

        [Fact]
        public void TestIsEmpty()
        {
            Assert.True(_generator.IsEmpty(null));
            Assert.True(_generator.IsEmpty(Guid.Empty));
            var guid = _generator.GenerateId(null, null);
            Assert.False(_generator.IsEmpty(guid));
        }

        [Fact]
        public void TestGuid()
        {
            var expectedTicks = DateTime.Now.Ticks;
            var expectedIncrement = 1000;
            var expectedMachineProcessId = new byte[] { 1, 32, 64, 128, 255 };
            var guid = (Guid)_generator.GenerateId(expectedTicks, expectedMachineProcessId, expectedIncrement);
            var bytes = guid.ToByteArray();
            var actualTicks = GetTicks(bytes);
            var actualMachineProcessId = GetMachineProcessId(bytes);
            var actualIncrement = GetIncrement(bytes);
            Assert.Equal(expectedTicks, actualTicks);
            Assert.True(expectedMachineProcessId.SequenceEqual(actualMachineProcessId));
            Assert.Equal(expectedIncrement, actualIncrement);
        }

        [Fact]
        public void TestGuidWithSpecifiedTicks()
        {
            var expectedTicks = 0x8000L;
            var expectedIncrement = 32;
            var expectedMachineProcessId = new byte[] { 1, 32, 64, 128, 255 };
            var guid = (Guid)_generator.GenerateId(expectedTicks, expectedMachineProcessId, expectedIncrement);
            var bytes = guid.ToByteArray();
            var actualTicks = GetTicks(bytes);
            var actualMachineProcessId = GetMachineProcessId(bytes);
            var actualIncrement = GetIncrement(bytes);
            Assert.Equal(expectedTicks, actualTicks);
            Assert.True(expectedMachineProcessId.SequenceEqual(actualMachineProcessId));
            Assert.Equal(expectedIncrement, actualIncrement);
        }

        private long GetTicks(byte[] bytes)
        {
            var a = (ulong)BitConverter.ToUInt32(bytes, 0);
            var b = (ulong)BitConverter.ToUInt16(bytes, 4);
            var c = (ulong)BitConverter.ToUInt16(bytes, 6);
            return (long)((a << 32) | (b << 16) | c);
        }

        private byte[] GetMachineProcessId(byte[] bytes)
        {
            var result = new byte[5];
            Array.Copy(bytes, 8, result, 0, 5);
            return result;
        }

        private int GetIncrement(byte[] bytes)
        {
            var increment = (bytes[13] << 16) + (bytes[14] << 8) + bytes[15];
            return increment;
        }
    }
}
