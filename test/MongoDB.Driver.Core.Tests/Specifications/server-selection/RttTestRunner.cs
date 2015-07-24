/* Copyright 2013-2015 MongoDB Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Specifications.server_selection
{
    [TestFixture]
    public class RttTestRunner
    {
        [TestCaseSource(typeof(TestCaseFactory), "GetTestCases")]
        public void RunTestDefinition(BsonDocument definition)
        {
            var subject = new ExponentiallyWeightedMovingAverage(0.2);

            var current = definition["avg_rtt_ms"];
            if (current.ToString() != "NULL")
            {
                subject.AddSample(TimeSpan.FromMilliseconds(current.ToDouble())); // the first value
            }

            var nextValue = definition["new_rtt_ms"].ToDouble();
            subject.AddSample(TimeSpan.FromMilliseconds(nextValue));
            var expected = definition["new_avg_rtt"].ToDouble();

            subject.Average.Should().BeCloseTo(TimeSpan.FromMilliseconds(expected), 1);
        }

        private static class TestCaseFactory
        {
            public static IEnumerable<ITestCaseData> GetTestCases()
            {
                const string prefix = "MongoDB.Driver.Specifications.server_selection.tests.rtt.";
                return Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .Select(path =>
                    {
                        var definition = ReadDefinition(path);
                        var fullName = path.Remove(0, prefix.Length);
                        var data = new TestCaseData(definition);
                        data.Categories.Add("Specifications");
                        data.Categories.Add("server-selection");
                        return data.SetName(fullName.Remove(fullName.Length - 5));
                    });
            }

            private static BsonDocument ReadDefinition(string path)
            {
                using (var definitionStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    return BsonDocument.Parse(definitionString);
                }
            }
        }
    }
}
