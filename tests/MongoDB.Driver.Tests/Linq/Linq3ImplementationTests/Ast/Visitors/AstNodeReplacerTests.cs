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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Visitors;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Ast.Visitors
{
    public class AstNodeReplacerTests
    {
        [Fact]
        public void Replace_should_return_expected_result()
        {
            var arg1 = AstExpression.Constant(1);
            var arg2 = AstExpression.Constant(2);
            var arg3 = AstExpression.Constant(3);
            var node = AstExpression.Eq(arg1, arg2);
            var mappings = new (AstNode Original, AstNode Replacement)[]
            {
                (arg1, arg2),
                (arg2, arg3)
            };

            var result = AstNodeReplacer.Replace(node, mappings);

            var binaryExpression = result.Should().BeOfType<AstBinaryExpression>().Subject;
            binaryExpression.Arg1.Should().BeSameAs(arg2);
            binaryExpression.Arg2.Should().BeSameAs(arg3);
        }
    }
}
