/* Copyright 2016 MongoDB Inc.
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
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.Bson.TestHelpers.XunitExtensions
{
    internal class SkippableTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly IMessageSink _diagnosticMessageSink;
        readonly TheoryDiscoverer _theoryDiscoverer;

        public SkippableTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;

            _theoryDiscoverer = new TheoryDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var defaultMethodDisplay = discoveryOptions.MethodDisplayOrDefault();

            // Unlike fact discovery, the underlying algorithm for theories is complex, so we let the theory discoverer
            // do its work, and do a little on-the-fly conversion into our own test cases.
            return _theoryDiscoverer.Discover(discoveryOptions, testMethod, factAttribute)
                .Select(testCase => testCase is XunitTheoryTestCase ? 
                    (IXunitTestCase)new SkippableTheoryTestCase(_diagnosticMessageSink, defaultMethodDisplay, testCase.TestMethod) :
                    new SkippableFactTestCase(_diagnosticMessageSink, defaultMethodDisplay, testCase.TestMethod, testCase.TestMethodArguments));
        }
    }
}
