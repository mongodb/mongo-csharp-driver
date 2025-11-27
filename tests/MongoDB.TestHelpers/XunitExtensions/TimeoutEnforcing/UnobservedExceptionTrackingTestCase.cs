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

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing;

public sealed class UnobservedExceptionTrackingTestCase : XunitTestCase
{
    public static readonly List<string> __unobservedExceptions = new();

#pragma warning disable CS0618 // Type or member is obsolete
    public UnobservedExceptionTrackingTestCase()
    {
    }
#pragma warning restore CS0618 // Type or member is obsolete

    public UnobservedExceptionTrackingTestCase(IMessageSink diagnosticMessageSink, ITestMethod testMethod)
        : base(diagnosticMessageSink, TestMethodDisplay.Method, TestMethodDisplayOptions.All, testMethod)
    {
        TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionEventHandler;
    }

    public override void Dispose()
    {
        base.Dispose();
        TaskScheduler.UnobservedTaskException -= UnobservedTaskExceptionEventHandler;
    }

    void UnobservedTaskExceptionEventHandler(object sender, UnobservedTaskExceptionEventArgs unobservedException) =>
        __unobservedExceptions.Add(unobservedException.Exception.ToString());
}

