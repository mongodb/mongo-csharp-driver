/* Copyright 2010-2015 MongoDB Inc.
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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.Linq.Processors;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Processors
{
    public class PartialEvaluatorTests
    {
        [Fact]
        public void Indexed_local_capture()
        {
            var captured = new[] { "Jack", "John" };
            Expression<Func<Person, bool>> predicate = x => x.Name == captured[0];

            var evaluated = (BinaryExpression)PartialEvaluator.Evaluate(predicate.Body);
            evaluated.Right.NodeType.Should().Be(ExpressionType.Constant);
            ((ConstantExpression)evaluated.Right).Value.Should().Be("Jack");
        }

        [Fact]
        public void Named_local_capture()
        {
            var captured = "Jack";
            Expression<Func<Person, bool>> predicate = x => x.Name == captured;

            var evaluated = (BinaryExpression)PartialEvaluator.Evaluate(predicate.Body);
            evaluated.Right.NodeType.Should().Be(ExpressionType.Constant);
            ((ConstantExpression)evaluated.Right).Value.Should().Be("Jack");
        }

        [Fact]
        public void Instance_method_call_with_no_arguments()
        {
            Expression<Func<Person, bool>> predicate = x => x.Name == InstanceGetName();

            var evaluated = (BinaryExpression)PartialEvaluator.Evaluate(predicate.Body);
            evaluated.Right.NodeType.Should().Be(ExpressionType.Constant);
            ((ConstantExpression)evaluated.Right).Value.Should().Be("Jack");
        }

        [Fact]
        public void Instance_method_call_with_a_constant_argument()
        {
            Expression<Func<Person, bool>> predicate = x => x.Name == InstanceGetName("ck");

            var evaluated = (BinaryExpression)PartialEvaluator.Evaluate(predicate.Body);
            evaluated.Right.NodeType.Should().Be(ExpressionType.Constant);
            ((ConstantExpression)evaluated.Right).Value.Should().Be("Jack");
        }

        [Fact]
        public void Instance_method_call_with_a_captured_argument()
        {
            var captured = "ck";
            Expression<Func<Person, bool>> predicate = x => x.Name == InstanceGetName(captured);

            var evaluated = (BinaryExpression)PartialEvaluator.Evaluate(predicate.Body);
            evaluated.Right.NodeType.Should().Be(ExpressionType.Constant);
            ((ConstantExpression)evaluated.Right).Value.Should().Be("Jack");
        }

        [Fact]
        public void Static_method_call_with_no_arguments()
        {
            Expression<Func<Person, bool>> predicate = x => x.Name == StaticGetName();

            var evaluated = (BinaryExpression)PartialEvaluator.Evaluate(predicate.Body);
            evaluated.Right.NodeType.Should().Be(ExpressionType.Constant);
            ((ConstantExpression)evaluated.Right).Value.Should().Be("Jack");
        }

        [Fact]
        public void Static_method_call_with_a_constant_argument()
        {
            Expression<Func<Person, bool>> predicate = x => x.Name == StaticGetName("ck");

            var evaluated = (BinaryExpression)PartialEvaluator.Evaluate(predicate.Body);
            evaluated.Right.NodeType.Should().Be(ExpressionType.Constant);
            ((ConstantExpression)evaluated.Right).Value.Should().Be("Jack");
        }

        [Fact]
        public void Static_method_call_with_a_captured_argument()
        {
            var captured = "ck";
            Expression<Func<Person, bool>> predicate = x => x.Name == StaticGetName(captured);

            var evaluated = (BinaryExpression)PartialEvaluator.Evaluate(predicate.Body);
            evaluated.Right.NodeType.Should().Be(ExpressionType.Constant);
            ((ConstantExpression)evaluated.Right).Value.Should().Be("Jack");
        }

        private string InstanceGetName()
        {
            return "Jack";
        }

        private string InstanceGetName(string suffix)
        {
            return "Ja" + suffix;
        }

        private static string StaticGetName()
        {
            return "Jack";
        }

        private static string StaticGetName(string suffix)
        {
            return "Ja" + suffix;
        }

        private class Person
        {
            public string Name { get; set; }
        }

    }
}
