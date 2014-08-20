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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class WriteConcernTests
    {
        [Test]
        public void Acknowledged_should_return_properly_initialized_instance()
        {
            var writeConcern = WriteConcern.Acknowledged;
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().BeNull();
            writeConcern.WTimeout.Should().NotHaveValue();
        }

        [TestCase(null, null, null, null)]
        [TestCase(1, null, null, null)]
        [TestCase(2, null, null, null)]
        [TestCase(null, 1, null, null)]
        [TestCase(null, 2, null, null)]
        [TestCase(null, null, false, null)]
        [TestCase(null, null, true, null)]
        [TestCase(null, null, null, false)]
        [TestCase(null, null, null, true)]
        public void Constructor_with_four_arguments_should_properly_initialize_instance(int? w, int? wTimeoutSeconds, bool? fsync, bool? journal)
        {
            var wCount = w.HasValue ? (WriteConcern.WValue)w.Value : null;
            var wTimeout = wTimeoutSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(wTimeoutSeconds.Value) : null;
            var writeConcern = new WriteConcern(wCount, wTimeout, fsync, journal);
            writeConcern.W.Should().Be(wCount);
            writeConcern.WTimeout.Should().Be(wTimeout);
            writeConcern.FSync.Should().Be(fsync);
            writeConcern.Journal.Should().Be(journal);
        }

        [Test]
        public void Constructor_with_int_argument_should_properly_initialize_instance()
        {
            var writeConcern = new WriteConcern(1);
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().Be((WriteConcern.WValue)1);
            writeConcern.WTimeout.Should().NotHaveValue();
        }

        [Test]
        public void Constructor_with_no_arguments_should_properly_initialize_instance()
        {
            var writeConcern = new WriteConcern();
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().BeNull();
            writeConcern.WTimeout.Should().NotHaveValue();
        }

        [Test]
        public void Constructor_with_string_argument_should_properly_initialize_instance()
        {
            var writeConcern = new WriteConcern("mode");
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().Be((WriteConcern.WValue)"mode");
            writeConcern.WTimeout.Should().NotHaveValue();
        }

        [TestCase(null, null, null, null)]
        [TestCase(1, null, null, null)]
        [TestCase(2, null, null, null)]
        [TestCase(null, 1, null, null)]
        [TestCase(null, 2, null, null)]
        [TestCase(null, null, false, null)]
        [TestCase(null, null, true, null)]
        [TestCase(null, null, null, false)]
        [TestCase(null, null, null, true)]
        public void Equals_should_return_true_if_all_fields_are_equal(int? w, int? wTimeoutSeconds, bool? fsync, bool? journal)
        {
            var wCount = w.HasValue ? (WriteConcern.WValue)w.Value : null;
            var wTimeout = wTimeoutSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(wTimeoutSeconds.Value) : null;
            var writeConcern1 = new WriteConcern(wCount, wTimeout, fsync, journal);
            var writeConcern2 = new WriteConcern(wCount, wTimeout, fsync, journal);
            writeConcern1.Equals((WriteConcern)writeConcern2).Should().BeTrue();
            writeConcern1.Equals((object)writeConcern2).Should().BeTrue();
            writeConcern1.GetHashCode().Should().Be(writeConcern2.GetHashCode());
        }

        [TestCase(null, false)]
        [TestCase(null, true)]
        [TestCase(false, null)]
        [TestCase(false, true)]
        [TestCase(true, null)]
        [TestCase(true, false)]
        public void Equals_should_return_false_if_fsync_is_not_equal(bool? fsync1, bool? fsync2)
        {
            var writeConcern1 = new WriteConcern(null, null, fsync1, null);
            var writeConcern2 = new WriteConcern(null, null, fsync2, null);
            writeConcern1.Equals((WriteConcern)writeConcern2).Should().BeFalse();
            writeConcern1.Equals((object)writeConcern2).Should().BeFalse();
            writeConcern1.GetHashCode().Should().NotBe(writeConcern2.GetHashCode());
        }

        [TestCase(null, false)]
        [TestCase(null, true)]
        [TestCase(false, null)]
        [TestCase(false, true)]
        [TestCase(true, null)]
        [TestCase(true, false)]
        public void Equals_should_return_false_if_journal_is_not_equal(bool? journal1, bool? journal2)
        {
            var writeConcern1 = new WriteConcern(null, null, null, journal1);
            var writeConcern2 = new WriteConcern(null, null, null, journal2);
            writeConcern1.Equals((WriteConcern)writeConcern2).Should().BeFalse();
            writeConcern1.Equals((object)writeConcern2).Should().BeFalse();
            writeConcern1.GetHashCode().Should().NotBe(writeConcern2.GetHashCode());
        }

        [TestCase(null, 1)]
        [TestCase(null, 2)]
        [TestCase(1, null)]
        [TestCase(1, 2)]
        [TestCase(2, null)]
        [TestCase(2, 1)]
        public void Equals_should_return_false_if_w_is_not_equal(int? w1, int? w2)
        {
            var wCount1 = w1.HasValue ? (WriteConcern.WValue)w1.Value : null;
            var wCount2 = w2.HasValue ? (WriteConcern.WValue)w2.Value : null;
            var writeConcern1 = new WriteConcern(wCount1, null, null, null);
            var writeConcern2 = new WriteConcern(wCount2, null, null, null);
            writeConcern1.Equals((WriteConcern)writeConcern2).Should().BeFalse();
            writeConcern1.Equals((object)writeConcern2).Should().BeFalse();
            writeConcern1.GetHashCode().Should().NotBe(writeConcern2.GetHashCode());
        }

        [TestCase(null, 1)]
        [TestCase(null, 2)]
        [TestCase(1, null)]
        [TestCase(1, 2)]
        [TestCase(2, null)]
        [TestCase(2, 1)]
        public void Equals_should_return_false_if_wTimeout_is_not_equal(int? wTimeout1Seconds, int? wTimeout2Seconds)
        {
            var wTimeout1 = wTimeout1Seconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(wTimeout1Seconds.Value) : null;
            var wTimeout2 = wTimeout2Seconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(wTimeout2Seconds.Value) : null;
            var writeConcern1 = new WriteConcern(null, wTimeout1, null, null);
            var writeConcern2 = new WriteConcern(null, wTimeout2, null, null);
            writeConcern1.Equals((WriteConcern)writeConcern2).Should().BeFalse();
            writeConcern1.Equals((object)writeConcern2).Should().BeFalse();
            writeConcern1.GetHashCode().Should().NotBe(writeConcern2.GetHashCode());
        }

        [TestCase(null, null, null, null, "{ }")]
        [TestCase(1, null, null, null, "{ w : 1 }")]
        [TestCase(null, 2, null, null, "{ wtimeout : 2s }")]
        [TestCase(null, null, true, null, "{ fsync : true }")]
        [TestCase(null, null, null, true, "{ journal : true }")]
        [TestCase(1, 2, true, true, "{ w : 1, wtimeout : 2s, fsync : true, journal : true }")]
        public void ToString_should_return_expected_value(int? wCount, int? wTimeoutSeconds, bool? fsync, bool? journal, string expected)
        {
            var wTimeout = wTimeoutSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(wTimeoutSeconds.Value) : null;
            var writeConcern = new WriteConcern(wCount, wTimeout, fsync, journal);
            writeConcern.ToString().Should().Be(expected);
        }

        [Test]
        public void Unacknowledged_should_return_properly_initialized_instance()
        {
            var writeConcern = WriteConcern.Unacknowledged;
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().Be((WriteConcern.WValue)0);
            writeConcern.WTimeout.Should().NotHaveValue();
        }

        [Test]
        public void W1_should_return_properly_initialized_instance()
        {
            var writeConcern = WriteConcern.W1;
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().Be((WriteConcern.WValue)1);
            writeConcern.WTimeout.Should().NotHaveValue();
        }

        [Test]
        public void W2_should_return_properly_initialized_instance()
        {
            var writeConcern = WriteConcern.W2;
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().Be((WriteConcern.WValue)2);
            writeConcern.WTimeout.Should().NotHaveValue();
        }

        [Test]
        public void W3_should_return_properly_initialized_instance()
        {
            var writeConcern = WriteConcern.W3;
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().Be((WriteConcern.WValue)3);
            writeConcern.WTimeout.Should().NotHaveValue();
        }

        [TestCase(null)]
        [TestCase(false)]
        [TestCase(true)]
        public void WithFSync_should_return_same_instance_if_value_is_equal(bool? fsync)
        {
            var writeConcern1 = new WriteConcern(null, null, fsync, null);
            var writeConcern2 = writeConcern1.WithFSync(fsync);
            writeConcern2.Should().BeSameAs(writeConcern1);
        }

        [TestCase(null, false)]
        [TestCase(null, true)]
        [TestCase(false, null)]
        [TestCase(false, true)]
        [TestCase(true, null)]
        [TestCase(true, false)]
        public void WithFSync_should_return_new_instance_if_value_is_not_equal(bool? fsync1, bool? fsync2)
        {
            var writeConcern1 = new WriteConcern(null, null, fsync1, null);
            var writeConcern2 = writeConcern1.WithFSync(fsync2);
            writeConcern2.Should().NotBeSameAs(writeConcern1);
            writeConcern2.FSync.Should().Be(fsync2);
        }

        [TestCase(null)]
        [TestCase(false)]
        [TestCase(true)]
        public void WithJournal_should_return_same_instance_if_value_is_equal(bool? journal)
        {
            var writeConcern1 = new WriteConcern(null, null, null, journal);
            var writeConcern2 = writeConcern1.WithJournal(journal);
            writeConcern2.Should().BeSameAs(writeConcern1);
        }

        [TestCase(null, false)]
        [TestCase(null, true)]
        [TestCase(false, null)]
        [TestCase(false, true)]
        [TestCase(true, null)]
        [TestCase(true, false)]
        public void WithJournal_should_return_new_instance_if_value_is_not_equal(bool? journal1, bool? journal2)
        {
            var writeConcern1 = new WriteConcern(null, null, null, journal1);
            var writeConcern2 = writeConcern1.WithJournal(journal2);
            writeConcern2.Should().NotBeSameAs(writeConcern1);
            writeConcern2.Journal.Should().Be(journal2);
        }

        [TestCase(null)]
        [TestCase(1)]
        [TestCase(2)]
        public void WithW_should_return_same_instance_if_value_is_equal(int? wCount)
        {
            var writeConcern1 = new WriteConcern(wCount, null, null, null);
            var writeConcern2 = writeConcern1.WithW(wCount);
            writeConcern2.Should().BeSameAs(writeConcern1);
        }

        [TestCase(null, 1)]
        [TestCase(null, 2)]
        [TestCase(1, null)]
        [TestCase(1, 2)]
        [TestCase(2, null)]
        [TestCase(2, 1)]
        public void WithW_should_return_new_instance_if_value_is_not_equal(int? wCount1, int? wCount2)
        {
            var writeConcern1 = new WriteConcern(wCount1, null, null, null);
            var writeConcern2 = writeConcern1.WithW(wCount2);
            writeConcern2.Should().NotBeSameAs(writeConcern1);
            writeConcern2.W.Should().Be((WriteConcern.WValue)wCount2);
        }

        [TestCase(null)]
        [TestCase(1)]
        [TestCase(2)]
        public void WithWTimeout_should_return_same_instance_if_value_is_equal(int? wTimeoutSeconds)
        {
            var wTimeout = wTimeoutSeconds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(wTimeoutSeconds.Value) : null;
            var writeConcern1 = new WriteConcern(null, wTimeout, null, null);
            var writeConcern2 = writeConcern1.WithWTimeout(wTimeout);
            writeConcern2.Should().BeSameAs(writeConcern1);
        }

        [TestCase(null, 1)]
        [TestCase(null, 2)]
        [TestCase(1, null)]
        [TestCase(1, 2)]
        [TestCase(2, null)]
        [TestCase(2, 1)]
        public void WithWTimeout_should_return_new_instance_if_value_is_not_equal(int? wTimeoutSeconds1, int? wTimeoutSeconds2)
        {
            var wTimeout1 = wTimeoutSeconds1.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(wTimeoutSeconds1.Value) : null;
            var wTimeout2 = wTimeoutSeconds2.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(wTimeoutSeconds2.Value) : null;
            var writeConcern1 = new WriteConcern(null, wTimeout1, null, null);
            var writeConcern2 = writeConcern1.WithWTimeout(wTimeout2);
            writeConcern2.Should().NotBeSameAs(writeConcern1);
            writeConcern2.WTimeout.Should().Be(wTimeout2);
        }

        [Test]
        public void WMajority_should_return_properly_initialized_instance()
        {
            var writeConcern = WriteConcern.WMajority;
            writeConcern.FSync.Should().NotHaveValue();
            writeConcern.Journal.Should().NotHaveValue();
            writeConcern.W.Should().Be((WriteConcern.WValue)"majority");
            writeConcern.WTimeout.Should().NotHaveValue();
        }
    }

    [TestFixture]
    public class WriteConcernWValueTests
    {
        [Test]
        public void Static_implicit_conversion_with_int_argument_returns_properly_initialized_instance()
        {
            WriteConcern.WValue wValue = 1;
            wValue.Should().BeOfType<WriteConcern.WCount>();
            ((WriteConcern.WCount)wValue).Value.Should().Be(1);
        }

        [Test]
        public void Static_implicit_conversion_with_null_int_argument_returns_properly_initialized_instance()
        {
            WriteConcern.WValue wValue = (int?)null;
            wValue.Should().BeNull();
        }

        [Test]
        public void Static_implicit_conversion_with_nullable_int_argument_returns_properly_initialized_instance()
        {
            WriteConcern.WValue wValue = (int?)1;
            wValue.Should().BeOfType<WriteConcern.WCount>();
            ((WriteConcern.WCount)wValue).Value.Should().Be(1);
        }

        [Test]
        public void Static_implicit_conversion_with_string_argument_returns_properly_initialized_instance()
        {
            WriteConcern.WValue wValue = "mode";
            wValue.Should().BeOfType<WriteConcern.WMode>();
            ((WriteConcern.WMode)wValue).Value.Should().Be("mode");
        }

        [Test]
        public void Static_implicit_conversion_with_null_string_argument_returns_properly_initialized_instance()
        {
            WriteConcern.WValue wValue = (string)null;
            wValue.Should().BeNull();
        }
    }

    [TestFixture]
    public class WriteConcernWCountTests
    {
        [TestCase(0)]
        [TestCase(1)]
        public void Constructor_should_initialize_instance(int w)
        {
            var wCount = new WriteConcern.WCount(w);
            wCount.Value.Should().Be(w);
        }

        [Test]
        public void Constructor_with_negative_value_should_throw()
        {
            Action action = () => new WriteConcern.WCount(-1);
            action.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        public void Equals_should_return_false_if_any_fields_are_not_equal(int w1, int w2)
        {
            var wCount1 = new WriteConcern.WCount(w1);
            var wCount2 = new WriteConcern.WCount(w2);
            wCount1.Equals(wCount2).Should().BeFalse();
            wCount1.Equals((object)wCount2).Should().BeFalse();
            wCount1.GetHashCode().Should().NotBe(wCount2.GetHashCode());
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Equals_should_return_true_if_all_fields_are_equal(int w)
        {
            var wCount1 = new WriteConcern.WCount(w);
            var wCount2 = new WriteConcern.WCount(w);
            wCount1.Equals(wCount2).Should().BeTrue();
            wCount1.Equals((object)wCount2).Should().BeTrue();
            wCount1.GetHashCode().Should().Be(wCount2.GetHashCode());
        }

        [Test]
        public void ToBsonValue_should_return_proper_value()
        {
            var wCount = new WriteConcern.WCount(1);
            var bsonValue = wCount.ToBsonValue();
            bsonValue.Should().BeOfType<BsonInt32>();
            bsonValue.AsInt32.Should().Be(1);
        }

        [Test]
        public void ToString_should_return_proper_value()
        {
            var wCount = new WriteConcern.WCount(1);
            wCount.ToString().Should().Be("1");
        }
    }

    [TestFixture]
    public class WriteConcernWModeTests
    {
        [Test]
        public void Constructor_should_initialize_instance()
        {
            var mode = new WriteConcern.WMode("mode");
            mode.Value.Should().Be("mode");
        }

        [Test]
        public void Constructor_with_empty_mode_should_throw()
        {
            Action action = () => new WriteConcern.WMode("");
            action.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Constructor_with_null_mode_should_throw()
        {
            Action action = () => new WriteConcern.WMode(null);
            action.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Majority_should_return_proper_value()
        {
            var mode = WriteConcern.WMode.Majority;
            mode.Value.Should().Be("majority");
        }

        [TestCase("mode1", "mode2")]
        [TestCase("mode2", "mode1")]
        public void Equals_should_return_false_if_values_are_not_equal(string value1, string value2)
        {
            var wMode1 = new WriteConcern.WMode(value1);
            var wMode2 = new WriteConcern.WMode(value2);
            wMode1.Equals(wMode2).Should().BeFalse();
            wMode1.Equals((object)wMode2).Should().BeFalse();
            wMode1.GetHashCode().Should().NotBe(wMode2.GetHashCode());
        }

        [Test]
        public void Equals_should_return_true_if_all_fields_are_equal()
        {
            var wMode1 = new WriteConcern.WMode("mode");
            var wMode2 = new WriteConcern.WMode("mode");
            wMode1.Equals(wMode2).Should().BeTrue();
            wMode1.Equals((object)wMode2).Should().BeTrue();
            wMode1.GetHashCode().Should().Be(wMode2.GetHashCode());
        }

        [Test]
        public void ToBsonValue_should_return_proper_value()
        {
            var wMode = new WriteConcern.WMode("mode");
            var bsonValue = wMode.ToBsonValue();
            bsonValue.Should().BeOfType<BsonString>();
            bsonValue.AsString.Should().Be("mode");
        }

        [Test]
        public void ToString_should_return_proper_value()
        {
            var wMode = new WriteConcern.WMode("mode");
            wMode.ToString().Should().Be("\"mode\"");
        }

        [Test]
        public void WValue_Parse_should_create_WCount_when_the_value_is_a_number()
        {
            var wValue = WriteConcern.WValue.Parse("2");

            wValue.Should().BeOfType<WriteConcern.WCount>();
            ((WriteConcern.WCount)wValue).Value.Should().Be(2);
        }

        [Test]
        public void WValue_Parse_should_create_WMode_when_value_is_not_a_number()
        {
            var wValue = WriteConcern.WValue.Parse("goofy");

            wValue.Should().BeOfType<WriteConcern.WMode>();
            ((WriteConcern.WMode)wValue).Value.Should().Be("goofy");
        }
    }
}
