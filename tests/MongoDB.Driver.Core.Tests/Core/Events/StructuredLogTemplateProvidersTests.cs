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
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Logging;
using Xunit;

namespace MongoDB.Driver.Core.Events
{
    public class StructuredLogTemplateProvidersTests
    {
        [Fact]
        public void All_events_should_have_template()
        {
            foreach (var eventType in Enum.GetValues(typeof(EventType)).Cast<EventType>())
            {
                var template = StructuredLogTemplateProviders.GetTemplateProvider(eventType);

                template.Templates.Count().Should().BeGreaterThan(0);
                template.Templates.First().Should().NotBeNull("Missing template for {0}", eventType);
                template.ParametersExtractor.Should().NotBeNull("Missing template for {0}", eventType);
            }
        }
    }
}
