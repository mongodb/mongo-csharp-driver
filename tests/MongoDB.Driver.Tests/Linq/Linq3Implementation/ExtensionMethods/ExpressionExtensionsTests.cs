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

using System;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.ExtensionMethods;

public class ExpressionExtensionsTests : IDisposable
{
    // One-shot LINQ evaluation lambdas are compiled with preferInterpretation: true to avoid a
    // .NET 10 tiered-JIT/GC race (CSHARP-6093). The interpreted path can be disabled at runtime via
    // this AppContext switch, reverting to lambda.Compile() (JIT). The observable result is identical
    // regardless of compile strategy, so these tests verify only that both paths are wired correctly.
    private const string SwitchName = "Switch.MongoDB.Driver.DisableLinqInterpretedEvaluation";

    [Fact]
    public void CompileForOneShotEvaluation_should_return_working_delegate_when_switch_unset()
    {
        AppContext.SetSwitch(SwitchName, false);
        var lambda = Expression.Lambda(Expression.Add(Expression.Constant(2), Expression.Constant(3)));

        var fn = lambda.CompileForOneShotEvaluation();

        fn.DynamicInvoke(null).Should().Be(5);
    }

    [Fact]
    public void CompileForOneShotEvaluation_should_return_working_delegate_when_interpretation_disabled()
    {
        AppContext.SetSwitch(SwitchName, true);
        var lambda = Expression.Lambda(Expression.Add(Expression.Constant(2), Expression.Constant(3)));

        var fn = lambda.CompileForOneShotEvaluation();

        fn.DynamicInvoke(null).Should().Be(5);
    }

    public void Dispose()
    {
        // Reset the global AppContext switch so it cannot leak into other tests.
        AppContext.SetSwitch(SwitchName, false);
    }
}
