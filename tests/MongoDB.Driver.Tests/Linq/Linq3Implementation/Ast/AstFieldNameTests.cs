/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Ast
{
    public class AstFieldNameTests
    {
        [Theory]
        [InlineData("x", true)]
        [InlineData("abc", true)]
        [InlineData("_id", true)]
        [InlineData("0", true)]
        [InlineData("a$b", true)] // a "$" only matters in the first position
        [InlineData("a b", true)]
        [InlineData("\u00e9t\u00e9", true)] // non-ASCII names are ordinary field names
        [InlineData("", false)] // the server rejects an empty field name
        [InlineData("$", false)]
        [InlineData("$ne", false)] // would be read as an operator in the first position of a document
        [InlineData("$where", false)]
        [InlineData("a.b", false)] // would be read as a path to a nested field
        [InlineData(".", false)]
        [InlineData("a.", false)]
        [InlineData(".b", false)]
        public void IsSafe_should_return_expected_result(string name, bool expectedResult)
        {
            var result = AstFieldName.IsSafe(name);

            result.Should().Be(expectedResult);
        }

    }
}
