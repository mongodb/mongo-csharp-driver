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

using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    public class IndexNameHelperTests
    {
        [Fact]
        public void GetIndexName_with_BsonDocument_should_return_expected_result()
        {
            var keys = new BsonDocument
            {
                { "a", new BsonDouble(1.0) },
                { "b", new BsonInt32(1) },
                { "c", new BsonInt64(1) },
                { "d", new BsonDouble(-1.0) },
                { "e", new BsonInt32(-1) },
                { "f", new BsonInt64(-1) },
                { "g g", "s s" },
                { "h", false }
            };
            var expectedResult = "a_1_b_1_c_1_d_-1_e_-1_f_-1_g_g_s_s_h_x";

            var result = IndexNameHelper.GetIndexName(keys);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void GetIndexName_with_names_should_return_expected_result()
        {
            var keys = new[] { "a", "b", "c c" };
            var expectedResult = "a_1_b_1_c_c_1";

            var result = IndexNameHelper.GetIndexName(keys);

            result.Should().Be(expectedResult);
        }
    }
}
