/* Copyright 2015-2016 MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class BsonDoubleTests
    {
        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_double_should_return_new_instance(
            [Values(-101.0, 101.0)]
            double value)
        {
            var result1 = (BsonDouble)value;
            var result2 = (BsonDouble)value;

            result2.Should().NotBeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_double_should_return_precreated_instance(
            [Range(-100.0, 100.0, 1.0)]
            double value)
        {
            var result1 = (BsonDouble)value;
            var result2 = (BsonDouble)value;

            result2.Should().BeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void precreated_instances_should_have_the_expected_value(
            [Range(-100.0, 100.0, 1.0)]
            double value)
        {
            var result = (BsonDouble)value;

            result.Value.Should().Be(value);
        }
    }
}
