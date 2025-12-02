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
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing;

[XunitTestCaseDiscoverer("MongoDB.TestHelpers.XunitExtensions.TimeoutEnforcing.UnobservedExceptionTestDiscoverer", "MongoDB.TestHelpers")]
public class UnobservedExceptionTrackingFactAttribute: FactAttribute
{}

public class UnobservedExceptionTestDiscoverer : IXunitTestCaseDiscoverer
{
    private readonly IMessageSink _diagnosticsMessageSink;

    public UnobservedExceptionTestDiscoverer(IMessageSink diagnosticsMessageSink)
    {
        _diagnosticsMessageSink = diagnosticsMessageSink;
        TaskScheduler.UnobservedTaskException += UnobservedTaskExceptionEventHandler;
    }

    public static readonly List<string> UnobservedExceptions = new();

    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return [new XunitTestCase(_diagnosticsMessageSink, TestMethodDisplay.Method, TestMethodDisplayOptions.All, testMethod)
        {
            Traits =
            {
                { "Category", ["UnobservedExceptionTracking"] }
            }
        }];
    }

    void UnobservedTaskExceptionEventHandler(object sender, UnobservedTaskExceptionEventArgs unobservedException) =>
        UnobservedExceptions.Add(unobservedException.Exception.ToString());
}

