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
using System.Globalization;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public interface IC
    {
        int Id { get; set; }
        TimeSpan T { get; set; }        
    }

    public static class TimeSpanSerializerTestsHelper<C> where C : IC, new()
    {
        public static void TestValue(long ticks, double value)
        {
            var jsonValue = JsonConvert.ToString(value);
            if (Regex.IsMatch(jsonValue, @"^-?\d+$")) { jsonValue += ".0"; } // if string looks like an integer add ".0" to match JsonWriter format
            TestValue(ticks, jsonValue);
        }

        public static void TestValue(long ticks, string jsonValue)
        {
            var c = new C { Id = 1, T = TimeSpan.FromTicks(ticks) };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'T' : # }".Replace("#", jsonValue).Replace("'", "\"");
            Assert.Equal(expected, json);

            var bson = c.ToBson();
            var rehydrated = BsonSerializer.Deserialize<C>(bson);
            Assert.Equal(1, rehydrated.Id);
            Assert.Equal(ticks, rehydrated.T.Ticks);
        }

        public static void TestUnderflow(long ticks, string jsonValue)
        { 
            var c = new C { Id = 1, T = TimeSpan.FromTicks(ticks) };
            var json = c.ToJson();
            var expected = "{ '_id' : 1, 'T' : # }".Replace("#", jsonValue).Replace("'", "\"");
            Assert.Equal(expected, json);
        }
    }

    public class TimeSpanSerializerDoubleDaysTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Days)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.0000000000011574074074074074)] // 1 Tick
        [InlineData(1.5)]
        [InlineData(11.5)]
        [InlineData(10675199.116730063)] // largest number of Days that can be represented by a TimeSpan
        [InlineData(-1.5)]
        [InlineData(-11.5)]
        [InlineData(-10675199.116730064)] // largest number of Days that can be represented by a TimeSpan
        public void TestDays(double days)
        {
            var ticks = (long)(days * TimeSpan.TicksPerDay);
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, days);
        }
    }

    public class TimeSpanSerializerDoubleHoursTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Hours)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.000000000027777777777777777)] // 1 Tick
        [InlineData(1.5)]
        [InlineData(11.5)]
        [InlineData(256204778.80152152)] // largest number of hours that can be represented in a TimeSpan
        [InlineData(-1.5)]
        [InlineData(-11.5)]
        [InlineData(-256204778.80152154)] // largest number of hours that can be represented in a TimeSpan
        public void TestHours(double hours)
        {
            var ticks = (long)(hours * TimeSpan.TicksPerHour);
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, hours);
        }
    }

    public class TimeSpanSerializerDoubleMicrosecondsTests
    {
        private static long __ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Microseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.1)] // 1 Tick
        [InlineData(1.5)]
        [InlineData(11.5)]
        [InlineData(92233720368547200.0)] // largest number of microseconds (approximately) that can be represented in a TimeSpan (and round tripped)
        [InlineData(-1.5)]
        [InlineData(-11.5)]
        [InlineData(-92233720368547200.0)] // largest number of microseconds (approximately) that can be represented in a TimeSpan (and round tripped)
        public void TestMicroseconds(double microseconds)
        {
            var ticks = (long)(microseconds * __ticksPerMicrosecond);
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, microseconds);
        }
    }

    public class TimeSpanSerializerDoubleMillisecondsTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Milliseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.0001)] // 1 Tick
        [InlineData(1.5)]
        [InlineData(11.5)]
        [InlineData(922337203685477.0)] // largest number of milliseconds that can be represented in a TimeSpan
        [InlineData(-1.5)]
        [InlineData(-11.5)]
        [InlineData(-922337203685477.0)] // largest number of milliseconds that can be represented in a TimeSpan
        public void TestMilliseconds(double milliseconds)
        {
            var ticks = (long)(milliseconds * TimeSpan.TicksPerMillisecond);
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, milliseconds);
        }
    }

    public class TimeSpanSerializerDoubleMinutesTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Minutes)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.0000000016666666666666667)] // 1 Tick
        [InlineData(1.5)]
        [InlineData(11.5)]
        [InlineData(15372286728.0)] // largest number of minutes that can be represented in a TimeSpan
        [InlineData(-1.5)]
        [InlineData(-11.5)]
        [InlineData(-15372286728.0)] // largest number of minutes that can be represented in a TimeSpan
        public void TestMinutes(double minutes)
        {
            var ticks = (long)(minutes * TimeSpan.TicksPerMinute);
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, minutes);
        }
    }

    public class TimeSpanSerializerDoubleNanosecondsTests
    {
        private const long __nanosecondsPerTick = 100;

        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Nanoseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0.0, "0.0")]
        [InlineData(100.0, "100.0")] // 1 Tick
        [InlineData(1100.0, "1100.0")] // only multiples of 100 can be round tripped
        [InlineData(92233720368547700.0, "92233720368547696.0")] // almost Int64.MaxValue
        [InlineData(-100.0, "-100.0")]
        [InlineData(-1100.0, "-1100.0")]
        [InlineData(-92233720368547700.0, "-92233720368547696.0")] // almost Int64.MinValue
        public void TestNanoseconds(double nanoseconds, string jsonValue)
        {
            var ticks = (long)(nanoseconds / __nanosecondsPerTick);
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, jsonValue);
        }
    }

    public class TimeSpanSerializerDoubleSecondsTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Seconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.0000001)] // 1 Tick
        [InlineData(1.5)]
        [InlineData(11.5)]
        [InlineData(922337203685.0)] // largest number of seconds that can be represented in a TimeSpan
        [InlineData(-1.5)]
        [InlineData(-11.5)]
        [InlineData(-922337203685.0)] // largest number of seconds that can be represented in a TimeSpan
        public void TestSeconds(double seconds)
        {
            var ticks = (long)(seconds * TimeSpan.TicksPerSecond);
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, seconds);
        }
    }

    public class TimeSpanSerializerDoubleTicksTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Double, TimeSpanUnits.Ticks)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0.0, "0.0")]
        [InlineData(1.0, "1.0")] // 1 Tick
        [InlineData(11.0, "11.0")]
        [InlineData(9223372036854774800.0, "9.2233720368547748E+18")] // almost Int64.MaxValue
        [InlineData(-1.0, "-1.0")]
        [InlineData(-11.0, "-11.0")]
        [InlineData(-9223372036854774800.0, "-9.2233720368547748E+18")] // almost Int64.MinValue
        public void TestTicks(double ticks, string jsonValue)
        {
            TimeSpanSerializerTestsHelper<C>.TestValue((long)ticks, jsonValue);
        }
    }

    public class TimeSpanSerializerInt32DaysTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Days)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(10675199)] // largest number of Days that can be represented by a TimeSpan
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-10675199)] // largest number of Days that can be represented by a TimeSpan
        public void TestDays(int days)
        {
            var ticks = days * TimeSpan.TicksPerDay;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, days.ToString());
        }

        [Fact]
        public void TestDaysUnderflow()
        {
            var ticks = TimeSpan.TicksPerDay - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "0");
        }
    }

    public class TimeSpanSerializerInt32HoursTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Hours)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(256204778)] // largest number of hours that can be represented in a TimeSpan
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-256204778)] // largest number of hours that can be represented in a TimeSpan
        public void TestHours(int hours)
        {
            var ticks = hours * TimeSpan.TicksPerHour;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, hours.ToString());
        }

        [Fact]
        public void TestHoursUnderflow()
        {
            var ticks = TimeSpan.TicksPerHour - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "0");
        }
    }

    public class TimeSpanSerializerInt32MicrosecondsTests
    {
        private static long __ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Microseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(2147483647)] // Int32.MaxValue
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-2147483648)] // Int32.MinValue
        public void TestMicroseconds(int microseconds)
        {
            var ticks = microseconds * __ticksPerMicrosecond;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, microseconds.ToString());
        }

        [Fact]
        public void TestMicrosecondsUnderflow()
        {
            var ticks = __ticksPerMicrosecond - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "0");
        }
    }

    public class TimeSpanSerializerInt32MillisecondsTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Milliseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(2147483647)] // Int32.MaxValue
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-2147483648)] // Int32.MinValue
        public void TestMilliseconds(int milliseconds)
        {
            var ticks = milliseconds * TimeSpan.TicksPerMillisecond;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, milliseconds.ToString());
        }

        [Fact]
        public void TestMillisecondsUnderflow()
        {
            var ticks = TimeSpan.TicksPerMillisecond - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "0");
        }
    }

    public class TimeSpanSerializerInt32MinutesTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Minutes)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(2147483647)] // Int32.MaxValue
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-2147483648)] // Int32.MinValue
        public void TestMinutes(int minutes)
        {
            var ticks = minutes * TimeSpan.TicksPerMinute;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, minutes.ToString());
        }

        [Fact]
        public void TestMinutesUnderflow()
        {
            var ticks = TimeSpan.TicksPerMinute - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "0");
        }
    }

    public class TimeSpanSerializerInt32NanosecondsTests
    {
        private const long __nanosecondsPerTick = 100;

        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Nanoseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)] // only multiples of 100 can be round tripped
        [InlineData(1100)]
        [InlineData(2147483600)] // almost Int32.MaxValue
        [InlineData(-100)]
        [InlineData(-1100)]
        [InlineData(-2147483600)] // almost Int32.MinValue
        public void TestNanoseconds(int nanoseconds)
        {
            var ticks = nanoseconds / __nanosecondsPerTick;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, nanoseconds.ToString());
        }
    }

    public class TimeSpanSerializerInt32SecondsTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Seconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(2147483647)] // Int32.MaxValue
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-2147483648)] // Int32.MinValue
        public void TestSeconds(int seconds)
        {
            var ticks = seconds * TimeSpan.TicksPerSecond;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, seconds.ToString());
        }

        [Fact]
        public void TestSecondsUnderflow()
        {
            var ticks = TimeSpan.TicksPerSecond - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "0");
        }
    }

    public class TimeSpanSerializerInt32TicksTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Ticks)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(2147483647)] // Int32.MaxValue
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-2147483648)] // Int32.MinValue
        public void TestTicks(int ticks)
        {
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, ticks.ToString());
        }
    }

    public class TimeSpanSerializerInt64DaysTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Days)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(10675199)] // largest number of Days that can be represented by a TimeSpan
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-10675199)] // largest number of Days that can be represented by a TimeSpan
        public void TestDays(long days)
        {
            var ticks = days * TimeSpan.TicksPerDay;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, string.Format("NumberLong({0})", days));
        }

        [Fact]
        public void TestDaysUnderflow()
        {
            var ticks = TimeSpan.TicksPerDay - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "NumberLong(0)");
        }
    }

    public class TimeSpanSerializerInt64HoursTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Hours)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(11)]
        [InlineData(256204778)] // largest number of hours that can be represented in a TimeSpan
        [InlineData(-1)]
        [InlineData(-11)]
        [InlineData(-256204778)] // largest number of hours that can be represented in a TimeSpan
        public void TestHours(long hours)
        {
            var ticks = hours * TimeSpan.TicksPerHour;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, string.Format("NumberLong({0})", hours));
        }

        [Fact]
        public void TestHoursUnderflow()
        {
            var ticks = TimeSpan.TicksPerHour - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "NumberLong(0)");
        }
    }

    public class TimeSpanSerializerInt64MicrosecondsTests
    {
        private static long __ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Microseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0, "NumberLong(0)")]
        [InlineData(1, "NumberLong(1)")]
        [InlineData(11, "NumberLong(11)")]
        [InlineData(922337203685477580, "NumberLong(\"922337203685477580\")")] // largest number of microseconds that can be represented in a TimeSpan
        [InlineData(-1, "NumberLong(-1)")]
        [InlineData(-11, "NumberLong(-11)")]
        [InlineData(-922337203685477580, "NumberLong(\"-922337203685477580\")")] // largest number of microseconds that can be represented in a TimeSpan
        public void TestMicroseconds(long microseconds, string jsonValue)
        {
            var ticks = microseconds * __ticksPerMicrosecond;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, jsonValue);
        }

        [Fact]
        public void TestMicrosecondsUnderflow()
        {
            var ticks = __ticksPerMicrosecond - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "NumberLong(0)");
        }
    }

    public class TimeSpanSerializerInt64MillisecondsTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Milliseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0, "NumberLong(0)")]
        [InlineData(1, "NumberLong(1)")]
        [InlineData(11, "NumberLong(11)")]
        [InlineData(922337203685477, "NumberLong(\"922337203685477\")")] // largest number of milliseconds that can be represented in a TimeSpan
        [InlineData(-1, "NumberLong(-1)")]
        [InlineData(-11, "NumberLong(-11)")]
        [InlineData(-922337203685477, "NumberLong(\"-922337203685477\")")] // largest number of milliseconds that can be represented in a TimeSpan
        public void TestMilliseconds(long milliseconds, string jsonValue)
        {
            var ticks = milliseconds * TimeSpan.TicksPerMillisecond;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, jsonValue);
        }

        [Fact]
        public void TestMillisecondsUnderflow()
        {
            var ticks = TimeSpan.TicksPerMillisecond - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "NumberLong(0)");
        }
    }

    public class TimeSpanSerializerInt64MinutesTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Minutes)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0, "NumberLong(0)")]
        [InlineData(1, "NumberLong(1)")]
        [InlineData(11, "NumberLong(11)")]
        [InlineData(15372286728, "NumberLong(\"15372286728\")")] // largest number of minutes that can be represented in a TimeSpan
        [InlineData(-1, "NumberLong(-1)")]
        [InlineData(-11, "NumberLong(-11)")]
        [InlineData(-15372286728, "NumberLong(\"-15372286728\")")] // largest number of minutes that can be represented in a TimeSpan
        public void TestMinutes(long minutes, string jsonValue)
        {
            var ticks = minutes * TimeSpan.TicksPerMinute;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, jsonValue);
        }

        [Fact]
        public void TestMinutesUnderflow()
        {
            var ticks = TimeSpan.TicksPerMinute - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "NumberLong(0)");
        }
    }

    public class TimeSpanSerializerInt64NanosecondsTests
    {
        private const long __nanosecondsPerTick = 100;

        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Nanoseconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0, "NumberLong(0)")]
        [InlineData(100, "NumberLong(100)")] // only multiples of 100 can be round tripped
        [InlineData(1100, "NumberLong(1100)")]
        [InlineData(9223372036854775800, "NumberLong(\"9223372036854775800\")")] // almost Int64.MaxValue
        [InlineData(-100, "NumberLong(-100)")]
        [InlineData(-1100, "NumberLong(-1100)")]
        [InlineData(-9223372036854775800, "NumberLong(\"-9223372036854775800\")")] // almost Int64.MinValue
        public void TestNanoseconds(long nanoseconds, string jsonValue)
        {
            var ticks = nanoseconds / __nanosecondsPerTick;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, jsonValue);
        }
    }

    public class TimeSpanSerializerInt64SecondsTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Seconds)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0, "NumberLong(0)")]
        [InlineData(1, "NumberLong(1)")]
        [InlineData(11, "NumberLong(11)")]
        [InlineData(922337203685, "NumberLong(\"922337203685\")")] // largest number of seconds that can be represented in a TimeSpan
        [InlineData(-1, "NumberLong(-1)")]
        [InlineData(-11, "NumberLong(-11)")]
        [InlineData(-922337203685, "NumberLong(\"-922337203685\")")] // largest number of seconds that can be represented in a TimeSpan
        public void TestSeconds(long seconds, string jsonValue)
        {
            var ticks = seconds * TimeSpan.TicksPerSecond;
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, jsonValue);
        }

        [Fact]
        public void TestSecondsUnderflow()
        {
            var ticks = TimeSpan.TicksPerSecond - 1;
            TimeSpanSerializerTestsHelper<C>.TestUnderflow(ticks, "NumberLong(0)");
        }
    }

    public class TimeSpanSerializerInt64TicksTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.Int64, TimeSpanUnits.Ticks)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0, "NumberLong(0)")]
        [InlineData(1, "NumberLong(1)")]
        [InlineData(11, "NumberLong(11)")]
        [InlineData(9223372036854775807, "NumberLong(\"9223372036854775807\")")] // Int64.MaxValue
        [InlineData(-1, "NumberLong(-1)")]
        [InlineData(-11, "NumberLong(-11)")]
        [InlineData(-9223372036854775808, "NumberLong(\"-9223372036854775808\")")] // Int64.MinValue
        public void TestTicks(long ticks, string jsonValue)
        {
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, jsonValue);
        }
    }

    public class TimeSpanSerializerStringTests
    {
        public class C : IC
        {
            public int Id { get; set; }
            [BsonTimeSpanOptions(BsonType.String)]
            public TimeSpan T { get; set; }
        }

        [Theory]
        [InlineData(0, "\"00:00:00\"")]
        [InlineData(1, "\"00:00:00.0000001\"")]
        [InlineData(10, "\"00:00:00.0000010\"")]
        [InlineData(10000, "\"00:00:00.0010000\"")]
        [InlineData(10000000, "\"00:00:01\"")]
        [InlineData(600000000, "\"00:01:00\"")]
        [InlineData(36000000000, "\"01:00:00\"")]
        [InlineData(864000000000, "\"1.00:00:00\"")]
        [InlineData(9223372036854775807, "\"10675199.02:48:05.4775807\"")] // long.MaxValue
        [InlineData(-1, "\"-00:00:00.0000001\"")]
        [InlineData(-10, "\"-00:00:00.0000010\"")]
        [InlineData(-10000, "\"-00:00:00.0010000\"")]
        [InlineData(-10000000, "\"-00:00:01\"")]
        [InlineData(-600000000, "\"-00:01:00\"")]
        [InlineData(-36000000000, "\"-01:00:00\"")]
        [InlineData(-864000000000, "\"-1.00:00:00\"")]
        [InlineData(-9223372036854775808, "\"-10675199.02:48:05.4775808\"")] // long.MinValue
        public void TestTicks(long ticks, string jsonValue)
        {
            TimeSpanSerializerTestsHelper<C>.TestValue(ticks, jsonValue);
        }
    }
}
