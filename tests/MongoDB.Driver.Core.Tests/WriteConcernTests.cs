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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver
{
    [Trait("Category", "WriteConcern")]
    public class WriteConcernTests
    {
        [Fact]
        public void Acknowledged_should_return_expected_result()
        {
            var result = WriteConcern.Acknowledged;

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().BeNull();
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_fsync_should_initialize_instance(
            [Values(false, true, null)]
            bool? fsync)
        {
            var result = new WriteConcern(fsync: fsync);

            result.FSync.Should().Be(fsync);
            result.Journal.Should().NotHaveValue();
            result.W.Should().BeNull();
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_journal_should_initialize_instance(
            [Values(false, true, null)]
            bool? journal)
        {
            var result = new WriteConcern(journal: journal);

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().Be(journal);
            result.W.Should().BeNull();
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_mode_and_fsync_should_initialize_instance(
            [Values("abc", "def")]
            string mode,
            [Values(false, true, null)]
            bool? fsync)
        {
            var result = new WriteConcern(mode, fsync: fsync);

            result.FSync.Should().Be(fsync);
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be(new WriteConcern.WMode(mode));
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_mode_and_journal_should_initialize_instance(
            [Values("abc", "def")]
            string mode,
            [Values(false, true, null)]
            bool? journal)
        {
            var result = new WriteConcern(mode, journal: journal);

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().Be(journal);
            result.W.Should().Be(new WriteConcern.WMode(mode));
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_mode_and_wTimeout_should_initialize_instance(
            [Values("abc", "def")]
            string mode,
            [Values(1, null)]
            int? wTimeoutSeconds)
        {
            var wTimeout = ToWTimeout(wTimeoutSeconds);

            var result = new WriteConcern(mode, wTimeout: wTimeout);

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be(new WriteConcern.WMode(mode));
            result.WTimeout.Should().Be(wTimeout);
        }

        [Fact]
        public void constructor_with_mode_should_initialize_instance()
        {
            var result = new WriteConcern("mode");

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be(new WriteConcern.WMode("mode"));
            result.WTimeout.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_with_mode_should_throw_when_mode_is_empty()
        {
            Action action = () => new WriteConcern(mode: "");

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("mode");
        }

        [Fact]
        public void constructor_with_mode_should_throw_when_mode_is_null()
        {
            Action action = () => new WriteConcern(mode: null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("mode");
        }

        [Fact]
        public void constructor_with_no_arguments_should_initialize_instance()
        {
            var result = new WriteConcern();

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().BeNull();
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_w_should_initialize_instance(
            [Values(0, 1)]
            int w)
        {
            var result = new WriteConcern(w);

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be(new WriteConcern.WCount(w));
            result.WTimeout.Should().NotHaveValue();
        }

        [Fact]
        public void constructor_with_w_should_throw_when_w_is_negative()
        {
            Action action = () => new WriteConcern(-1);

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("w");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_w_and_fsync_should_initialize_instance(
            [Values(0, 1)]
            int w,
            [Values(false, true, null)]
            bool? fsync)
        {
            var result = new WriteConcern(w, fsync: fsync);

            result.FSync.Should().Be(fsync);
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be(new WriteConcern.WCount(w));
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_w_and_journal_should_initialize_instance(
            [Values(0, 1)]
            int w,
            [Values(false, true, null)]
            bool? journal)
        {
            var result = new WriteConcern(w, journal: journal);

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().Be(journal);
            result.W.Should().Be(new WriteConcern.WCount(w));
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_w_and_wTimeout_should_initialize_instance(
            [Values(0, 1)]
            int w,
            [Values(1, null)]
            int? wTimeoutSeconds)
        {
            var wTimeout = ToWTimeout(wTimeoutSeconds);

            var result = new WriteConcern(w, wTimeout: wTimeout);

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be(new WriteConcern.WCount(w));
            result.WTimeout.Should().Be(wTimeout);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_wTimeout_should_initialize_instance(
            [Values(1, null)]
            int? wTimeoutSeconds)
        {
            var wTimeout = ToWTimeout(wTimeoutSeconds);

            var result = new WriteConcern(wTimeout: wTimeout);

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().BeNull();
            result.WTimeout.Should().Be(wTimeout);
        }

        [Fact]
        public void constructor_with_wTimeout_should_throw_when_wTimeout_is_negative()
        {
            Action action = () => new WriteConcern(wTimeout: TimeSpan.FromSeconds(-1));

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("wTimeout");
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_wValue_should_initialize_instance(
            [Values(1, "abc", null)]
            object w)
        {
            var wValue = ToWValue(w);

            var result = new WriteConcern(wValue);

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be(wValue);
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void Equals_should_return_false_when_any_fields_are_not_equal(
            [Values("fsync", "journal", "w", "wTimeout")]
            string notEqualFieldName)
        {
            var subject1 = new WriteConcern(1, TimeSpan.FromSeconds(1), false, false);
            WriteConcern subject2;
            switch (notEqualFieldName)
            {
                case "fsync": subject2 = subject1.With(fsync: true); break;
                case "journal": subject2 = subject1.With(journal: true); break;
                case "w": subject2 = subject1.With(w: 2); break;
                case "wTimeout": subject2 = subject1.With(wTimeout: TimeSpan.FromSeconds(2)); break;
                default: throw new ArgumentException("notEqualFieldName");
            }

            var result1 = subject1.Equals(subject2);
            var result2 = subject1.Equals((object)subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result1.Should().BeFalse();
            result2.Should().BeFalse();
            hashCode1.Should().NotBe(hashCode2);
        }

        [Fact]
        public void Equals_should_return_false_when_other_is_null()
        {
            var subject = new WriteConcern(1, TimeSpan.FromSeconds(1), false, false);

            var result1 = subject.Equals((WriteConcern)null);
            var result2 = subject.Equals((object)null);

            result1.Should().BeFalse();
            result2.Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_when_other_is_wrong_type()
        {
            var subject = new WriteConcern(1, TimeSpan.FromSeconds(1), false, false);
            var other = new object();

            var result = subject.Equals(other);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_true_when_all_fields_are_equal()
        {
            var subject1 = new WriteConcern(1, TimeSpan.FromSeconds(1), false, false);
            var subject2 = new WriteConcern(1, TimeSpan.FromSeconds(1), false, false);

            var result1 = subject1.Equals(subject2);
            var result2 = subject1.Equals((object)subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result1.Should().BeTrue();
            result2.Should().BeTrue();
            hashCode1.Should().Be(hashCode2);
        }

        [Theory]
        [InlineData(0, null, null, null, false)]
        [InlineData(1, null, null, null, true)]
        [InlineData(0, false, null, null, false)]
        [InlineData(0, true, null, null, true)]
        [InlineData(0, null, false, null, false)]
        [InlineData(0, null, true, null, true)]
        [InlineData(0, null, null, 1, false)]
        public void IsAcknowledged_should_return_expected_result_with_w(
            int? w,
            bool? fsync,
            bool? journal,
            int? wTimeoutSeconds,
            bool expectedResult)
        {
            var wValue = ToWValue(w);
            var wTimeout = ToWTimeout(wTimeoutSeconds);
            var subject = new WriteConcern(wValue, fsync: fsync, journal: journal, wTimeout: wTimeout);

            var result = subject.IsAcknowledged;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(false, null, null)]
        [InlineData(true, null, null)]
        [InlineData(null, false, null)]
        [InlineData(null, true, null)]
        [InlineData(null, null, 1)]
        public void IsAcknowledged_should_return_expected_result_with_mode(
            bool? fsync,
            bool? journal,
            int? wTimeoutSeconds)
        {
            var wTimeout = ToWTimeout(wTimeoutSeconds);
            var subject = new WriteConcern("mode", fsync: fsync, journal: journal, wTimeout: wTimeout);

            var result = subject.IsAcknowledged;

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(null, null, null, null, "{ }")]
        [InlineData(1, null, null, null, "{ w : 1 }")]
        [InlineData(null, 2, null, null, "{ wtimeout : 2000 }")]
        [InlineData(null, null, true, null, "{ fsync : true }")]
        [InlineData(null, null, null, true, "{ j : true }")]
        [InlineData(1, 2, true, true, "{ w : 1, wtimeout : 2000, fsync : true, j : true }")]
        [InlineData("majority", 2, true, true, "{ w : \"majority\", wtimeout : 2000, fsync : true, j : true }")]
        public void ToBsonDocument_should_return_expected_result(object w, int? wTimeoutSeconds, bool? fsync, bool? journal, string expectedResult)
        {
            var wValue = ToWValue(w);
            var wTimeout = ToWTimeout(wTimeoutSeconds);
            var subject = new WriteConcern(wValue, wTimeout, fsync, journal);

            var result = subject.ToBsonDocument();

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(null, null, null, null, "{ }")]
        [InlineData(1, null, null, null, "{ w : 1 }")]
        [InlineData(null, 2, null, null, "{ wtimeout : 2s }")]
        [InlineData(null, null, true, null, "{ fsync : true }")]
        [InlineData(null, null, null, true, "{ journal : true }")]
        [InlineData(1, 2, true, true, "{ w : 1, wtimeout : 2s, fsync : true, journal : true }")]
        [InlineData("majority", 2, true, true, "{ w : \"majority\", wtimeout : 2s, fsync : true, journal : true }")]
        public void ToString_should_return_expected_result(object w, int? wTimeoutSeconds, bool? fsync, bool? journal, string expectedResult)
        {
            var wValue = ToWValue(w);
            var wTimeout = ToWTimeout(wTimeoutSeconds);
            var subject = new WriteConcern(wValue, wTimeout, fsync, journal);

            var result = subject.ToString();

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Unacknowledged_should_return_expected_result()
        {
            var result = WriteConcern.Unacknowledged;

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be((WriteConcern.WValue)0);
            result.WTimeout.Should().NotHaveValue();
        }

        [Fact]
        public void W1_should_return_expected_result()
        {
            var result = WriteConcern.W1;

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be((WriteConcern.WValue)1);
            result.WTimeout.Should().NotHaveValue();
        }

        [Fact]
        public void W2_should_return_expected_result()
        {
            var result = WriteConcern.W2;

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be((WriteConcern.WValue)2);
            result.WTimeout.Should().NotHaveValue();
        }

        [Fact]
        public void W3_should_return_expected_result()
        {
            var result = WriteConcern.W3;

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be((WriteConcern.WValue)3);
            result.WTimeout.Should().NotHaveValue();
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_new_instance_when_any_value_is_not_equal(
            [Values("w", "wTimeout", "fsync", "journal")]
            string notEqualFieldName)
        {
            var w = (WriteConcern.WValue)1;
            var wTimeout = TimeSpan.FromSeconds(2);
            var fsync = false;
            var journal = false;
            var subject = new WriteConcern(w, wTimeout, fsync, journal);
            switch (notEqualFieldName)
            {
                case "w": w = (WriteConcern.WValue)2; break;
                case "wTimeout": wTimeout = TimeSpan.FromSeconds(3); break;
                case "fsync": fsync = true; break;
                case "journal": journal = true; break;
                default: throw new ArgumentException("notEqualFieldName");
            }

            var result = subject.With(w, wTimeout, fsync, journal);

            result.Should().NotBeSameAs(subject);
            result.W.Should().Be(w);
            result.WTimeout.Should().Be(wTimeout);
            result.FSync.Should().Be(fsync);
            result.Journal.Should().Be(journal);
        }

        [Fact]
        public void With_should_return_same_instance_when_all_values_are_equal()
        {
            var subject = new WriteConcern(1, TimeSpan.FromSeconds(2), true, true);

            var result = subject.With(1, TimeSpan.FromSeconds(2), true, true);

            result.Should().BeSameAs(subject);
        }

        [Fact]
        public void With_should_return_same_instance_when_no_values_are_provided()
        {
            var subject = new WriteConcern();

            var result = subject.With();

            result.Should().BeSameAs(subject);
        }

        [Theory]
        [ParameterAttributeData]
        public void With_should_return_same_instance_when_value_is_equal(
            [Values("w", "mode", "wValue", "wTimeout", "fsync", "journal")]
            string fieldName)
        {
            var wValue = fieldName == "mode" ? (WriteConcern.WValue)new WriteConcern.WMode("mode") : new WriteConcern.WCount(1);
            var wTimeout = TimeSpan.FromSeconds(2);
            var fsync = false;
            var journal = true;
            var subject = new WriteConcern(wValue, wTimeout, fsync, journal);

            WriteConcern result;
            switch (fieldName)
            {
                case "w": result = subject.With(w: 1); break;
                case "mode": result = subject.With(mode: "mode"); break;
                case "wValue": result = subject.With(w: wValue); break;
                case "wTimeout": result = subject.With(wTimeout: wTimeout); break;
                case "fsync": result = subject.With(fsync: fsync); break;
                case "journal": result = subject.With(journal: journal); break;
                default: throw new ArgumentException("providedFieldName");
            }

            result.Should().BeSameAs(subject);
        }

        [Fact]
        public void WMajority_should_return_expected_result()
        {
            var result = WriteConcern.WMajority;

            result.FSync.Should().NotHaveValue();
            result.Journal.Should().NotHaveValue();
            result.W.Should().Be((WriteConcern.WMode)"majority");
            result.WTimeout.Should().NotHaveValue();
        }

        // helper methods
        private TimeSpan? ToWTimeout(int? wtimeoutSeconds)
        {
            if (wtimeoutSeconds.HasValue)
            {
                return TimeSpan.FromSeconds(wtimeoutSeconds.Value);
            }
            else
            {
                return null;
            }
        }

        private WriteConcern.WValue ToWValue(object w)
        {
            if (w == null)
            {
                return null;
            }
            else if (w is int)
            {
                return new WriteConcern.WCount((int)w);
            }
            else
            {
                return new WriteConcern.WMode((string)w);
            }
        }
    }

    public class WriteConcernWValueTests
    {
        [Fact]
        public void implicit_conversion_from_int_should_return_expected_result()
        {
            WriteConcern.WValue result = 1;

            result.Should().BeOfType<WriteConcern.WCount>();
            ((WriteConcern.WCount)result).Value.Should().Be(1);
        }

        [Fact]
        public void implicit_conversion_from_nullable_int_should_return_expected_result_when_value_is_not_null()
        {
            WriteConcern.WValue result = (int?)1;

            result.Should().BeOfType<WriteConcern.WCount>();
            ((WriteConcern.WCount)result).Value.Should().Be(1);
        }

        [Fact]
        public void implicit_conversion_from_nullable_int_should_return_expected_result_when_value_is_null()
        {
            WriteConcern.WValue result = (int?)null;

            result.Should().BeNull();
        }

        [Fact]
        public void implicit_conversion_from_string_should_return_expected_result_when_value_is_not_null()
        {
            WriteConcern.WValue result = (string)null;

            result.Should().BeNull();
        }

        [Fact]
        public void implicit_conversion_from_string_should_return_expected_result_when_value_is_null()
        {
            WriteConcern.WValue result = "mode";

            result.Should().BeOfType<WriteConcern.WMode>();
            ((WriteConcern.WMode)result).Value.Should().Be("mode");
        }

        [Fact]
        public void Parse_should_return_expected_result_when_value_is_not_numeric()
        {
            var result = WriteConcern.WValue.Parse("mode");

            result.Should().Be(new WriteConcern.WMode("mode"));
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        public void Parse_should_return_expected_result_when_value_is_numeric(
            string value,
            int w)
        {
            var result = WriteConcern.WValue.Parse(value);

            result.Should().Be(new WriteConcern.WCount(w));
        }
    }

    public class WriteConcernWCountTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void constructor_should_initialize_instance(int w)
        {
            var result = new WriteConcern.WCount(w);

            result.Value.Should().Be(w);
        }

        [Fact]
        public void constructor_should_throw_when_w_is_negative()
        {
            Action action = () => new WriteConcern.WCount(-1);

            action.ShouldThrow<ArgumentOutOfRangeException>().And.ParamName.Should().Be("w");
        }

        [Fact]
        public void Equals_should_return_false_if_any_fields_are_not_equal()
        {
            var subject1 = new WriteConcern.WCount(0);
            var subject2 = new WriteConcern.WCount(1);

            var result1 = subject1.Equals(subject2);
            var result2 = subject1.Equals((object)subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result1.Should().BeFalse();
            result2.Should().BeFalse();
            hashCode1.Should().NotBe(hashCode2);
        }

        [Fact]
        public void Equals_should_return_true_if_all_fields_are_equal()
        {
            var subject1 = new WriteConcern.WCount(1);
            var subject2 = new WriteConcern.WCount(1);

            var result1 = subject1.Equals(subject2);
            var result2 = subject1.Equals((object)subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result1.Should().BeTrue();
            result2.Should().BeTrue();
            hashCode1.Should().Be(hashCode2);
        }

        [Fact]
        public void ToBsonValue_should_return_expected_result()
        {
            var subject = new WriteConcern.WCount(1);

            var result = subject.ToBsonValue();

            result.Should().BeOfType<BsonInt32>();
            result.AsInt32.Should().Be(1);
        }

        [Fact]
        public void ToString_should_return_expected_result()
        {
            var subject = new WriteConcern.WCount(1);

            var result = subject.ToString();

            result.Should().Be("1");
        }
    }

    public class WriteConcernWModeTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var result = new WriteConcern.WMode("mode");

            result.Value.Should().Be("mode");
        }

        [Fact]
        public void constructor_should_throw_when_mode_is_empty()
        {
            Action action = () => new WriteConcern.WMode("");

            action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("mode");
        }

        [Fact]
        public void constructor_should_throw_when_mode_is_null()
        {
            Action action = () => new WriteConcern.WMode(null);

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("mode");
        }

        [Fact]
        public void Majority_should_return_expected_result()
        {
            var result = WriteConcern.WMode.Majority;

            result.Value.Should().Be("majority");
        }

        [Fact]
        public void Equals_should_return_false_when_any_fields_are_not_equal()
        {
            var subject1 = new WriteConcern.WMode("mode1");
            var subject2 = new WriteConcern.WMode("mode2");

            var result1 = subject1.Equals(subject2);
            var result2 = subject1.Equals((object)subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result1.Should().BeFalse();
            result2.Should().BeFalse();
            hashCode1.Should().NotBe(hashCode2);
        }

        [Fact]
        public void Equals_should_return_true_when_all_fields_are_equal()
        {
            var subject1 = new WriteConcern.WMode("mode");
            var subject2 = new WriteConcern.WMode("mode");

            var result1 = subject1.Equals(subject2);
            var result2 = subject1.Equals((object)subject2);
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            result1.Should().BeTrue();
            result2.Should().BeTrue();
            hashCode1.Should().Be(hashCode2);
        }

        [Fact]
        public void ToBsonValue_should_return_expected_result()
        {
            var subject = new WriteConcern.WMode("mode");

            var result = subject.ToBsonValue();

            result.Should().BeOfType<BsonString>();
            result.AsString.Should().Be("mode");
        }

        [Fact]
        public void ToString_should_return_expected_result()
        {
            var subject = new WriteConcern.WMode("mode");

            var result = subject.ToString();

            result.Should().Be("mode");
        }
    }
}
