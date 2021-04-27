/* Copyright 2021-present MongoDB Inc.
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
* 
*/

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
#if NETCOREAPP1_1
using System.Reflection;
#endif
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq
{
    public class MethodHelperTests
    {
        [Fact]
        public void GetMethodInfo_should_throw_when_lambda_is_null()
        {
            Expression<Func<object>> expression = null;

            var exception = Record.Exception(() => MethodHelper.GetMethodInfo(expression));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("lambda");
        }

        [Fact]
        public void GetMethodInfo_should_throw_when_lambda_is_not_a_method_call()
        {
            var exception = Record.Exception(() => MethodHelper.GetMethodInfo(() => true));

            var mongoInternalException = exception.Should().BeOfType<MongoInternalException>().Subject;
            mongoInternalException.Message.Should().Be("Unable to extract method info from True");
        }

        [Fact]
        public void GetMethodInfo_should_return_expected_result_for_non_generic_method()
        {
            var result = MethodHelper.GetMethodInfo(() => new object().GetHashCode());

            var methodInfo = typeof(object).GetMethod(nameof(GetHashCode));
            result.ShouldBeEquivalentTo(methodInfo);
        }

        [Fact]
        public void GetMethodInfo_should_return_expected_result_for_generic_method()
        {
            var result = MethodHelper.GetMethodInfo(() => new List<object>().Find(null));

            var methodInfo = typeof(List<object>).GetMethod(nameof(List<object>.Find));
            result.ShouldBeEquivalentTo(methodInfo);
        }

        [Fact]
        public void GetMethodInfo_T_should_throw_when_lambda_is_null()
        {
            Expression<Func<object, object>> e = null;

            var exception = Record.Exception(() => MethodHelper.GetMethodInfo(e));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("lambda");
        }

        [Fact]
        public void GetMethodInfo_T_should_throw_when_lambda_is_not_a_method_call()
        {
            var exception = Record.Exception(() => MethodHelper.GetMethodInfo((object x) => true));

            var mongoInternalException = exception.Should().BeOfType<MongoInternalException>().Subject;
            mongoInternalException.Message.Should().Be("Unable to extract method info from True");
        }

        [Fact]
        public void GetMethodInfo_T_should_return_expected_result_for_non_generic_method()
        {
            var result = MethodHelper.GetMethodInfo((object o) => o.GetHashCode());

            var methodInfo = typeof(object).GetMethod(nameof(GetHashCode));
            result.ShouldBeEquivalentTo(methodInfo);
        }

        [Fact]
        public void GetMethodInfo_T_should_return_expected_result_for_generic_method()
        {
            var result = MethodHelper.GetMethodInfo((List<object> l) => l.Find(null));

            var methodInfo = typeof(List<object>).GetMethod(nameof(List<object>.Find));
            result.ShouldBeEquivalentTo(methodInfo);
        }

        [Fact]
        public void GetMethodDefinition_with_ambiguous_method_signature_should_not_fail()
        {
            var methodInfo = MethodHelper.GetMethodInfo(() => new TestClass<object, string>().TestMethod(new object()));

            var exception = Record.Exception(() => MethodHelper.GetMethodDefinition(methodInfo));

#if NETCOREAPP1_1
            // We are aware that netstandard 1.5 target path does not distinguish these methods.
            // This will go away when netstandard 1.5 target is dropped.
            exception.Should().BeOfType<InvalidOperationException>();
#else
            exception.Should().BeNull();
#endif
        }

        private class TestClass<T1, T2>
        {
            public T1 TestMethod(T1 t1)
            {
                return t1;
            }

            public T2 TestMethod(T2 t2)
            {
                return t2;
            }
        }
    }
}
