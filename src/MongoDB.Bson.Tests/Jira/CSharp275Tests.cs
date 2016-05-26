/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp275Tests
    {
        private class Test
        {
            public string Json;
            public string Iso;
            public Test(string json, string iso)
            {
                this.Json = json;
                this.Iso = iso;
            }
        }

        private Test[] _tests = new Test[]
        {
            // note: use EST/EDT in all Json values to ensure DateTime.Parse doesn't work
            // test with dayOfWeek
            new Test("Mon, 10 Oct 2011 11:22:33 EDT", "2011-10-10T11:22:33-04:00"),
            new Test("Tue, 11 Oct 2011 11:22:33 EDT", "2011-10-11T11:22:33-04:00"),
            new Test("Wed, 12 Oct 2011 11:22:33 EDT", "2011-10-12T11:22:33-04:00"),
            new Test("Thu, 13 Oct 2011 11:22:33 EDT", "2011-10-13T11:22:33-04:00"),
            new Test("Fri, 14 Oct 2011 11:22:33 EDT", "2011-10-14T11:22:33-04:00"),
            new Test("Sat, 15 Oct 2011 11:22:33 EDT", "2011-10-15T11:22:33-04:00"),
            new Test("Sun, 16 Oct 2011 11:22:33 EDT", "2011-10-16T11:22:33-04:00"),
            // test without dayOfWeek
            new Test("10 Oct 2011 11:22:33 EDT", "2011-10-10T11:22:33-04:00"),
            new Test("11 Oct 2011 11:22:33 EDT", "2011-10-11T11:22:33-04:00"),
            new Test("12 Oct 2011 11:22:33 EDT", "2011-10-12T11:22:33-04:00"),
            new Test("13 Oct 2011 11:22:33 EDT", "2011-10-13T11:22:33-04:00"),
            new Test("14 Oct 2011 11:22:33 EDT", "2011-10-14T11:22:33-04:00"),
            new Test("15 Oct 2011 11:22:33 EDT", "2011-10-15T11:22:33-04:00"),
            new Test("16 Oct 2011 11:22:33 EDT", "2011-10-16T11:22:33-04:00"),
            // test monthName
            new Test("1 Jan 2011 11:22:33 EST", "2011-01-01T11:22:33-05:00"),
            new Test("1 Feb 2011 11:22:33 EST", "2011-02-01T11:22:33-05:00"),
            new Test("1 Mar 2011 11:22:33 EST", "2011-03-01T11:22:33-05:00"),
            new Test("1 Apr 2011 11:22:33 EDT", "2011-04-01T11:22:33-04:00"),
            new Test("1 May 2011 11:22:33 EDT", "2011-05-01T11:22:33-04:00"),
            new Test("1 Jun 2011 11:22:33 EDT", "2011-06-01T11:22:33-04:00"),
            new Test("1 Jul 2011 11:22:33 EDT", "2011-07-01T11:22:33-04:00"),
            new Test("1 Aug 2011 11:22:33 EDT", "2011-08-01T11:22:33-04:00"),
            new Test("1 Sep 2011 11:22:33 EDT", "2011-09-01T11:22:33-04:00"),
            new Test("1 Oct 2011 11:22:33 EDT", "2011-10-01T11:22:33-04:00"),
            new Test("1 Nov 2011 11:22:33 EDT", "2011-11-01T11:22:33-04:00"),
            new Test("1 Dec 2011 11:22:33 EST", "2011-12-01T11:22:33-05:00"),
            // test 2-digit year
            new Test("Mon, 1 Jan 01 11:22:33 EST", "2001-01-01T11:22:33-5:00"),
            new Test("Mon, 1 Jan 29 11:22:33 EST", "2029-01-01T11:22:33-5:00"),
            new Test("Tue, 1 Jan 30 11:22:33 EST", "2030-01-01T11:22:33-5:00"),
            new Test("Wed, 1 Jan 31 11:22:33 EST", "2031-01-01T11:22:33-5:00"),
            new Test("Thu, 1 Jan 32 11:22:33 EST", "2032-01-01T11:22:33-5:00"),
            new Test("Fri, 1 Jan 99 11:22:33 EST", "1999-01-01T11:22:33-5:00"),
            // test time zones
            new Test("Mon, 10 Oct 2011 11:22:33 UT", "2011-10-10T11:22:33-00:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 GMT", "2011-10-10T11:22:33-00:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 EST", "2011-10-10T11:22:33-05:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 EDT", "2011-10-10T11:22:33-04:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 CST", "2011-10-10T11:22:33-06:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 CDT", "2011-10-10T11:22:33-05:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 MST", "2011-10-10T11:22:33-07:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 MDT", "2011-10-10T11:22:33-06:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 PST", "2011-10-10T11:22:33-08:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 PDT", "2011-10-10T11:22:33-07:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 A", "2011-10-10T11:22:33-01:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 B", "2011-10-10T11:22:33-02:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 C", "2011-10-10T11:22:33-03:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 D", "2011-10-10T11:22:33-04:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 E", "2011-10-10T11:22:33-05:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 F", "2011-10-10T11:22:33-06:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 G", "2011-10-10T11:22:33-07:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 H", "2011-10-10T11:22:33-08:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 I", "2011-10-10T11:22:33-09:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 K", "2011-10-10T11:22:33-10:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 L", "2011-10-10T11:22:33-11:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 M", "2011-10-10T11:22:33-12:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 N", "2011-10-10T11:22:33+01:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 O", "2011-10-10T11:22:33+02:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 P", "2011-10-10T11:22:33+03:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 Q", "2011-10-10T11:22:33+04:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 R", "2011-10-10T11:22:33+05:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 S", "2011-10-10T11:22:33+06:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 T", "2011-10-10T11:22:33+07:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 U", "2011-10-10T11:22:33+08:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 V", "2011-10-10T11:22:33+09:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 W", "2011-10-10T11:22:33+10:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 X", "2011-10-10T11:22:33+11:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 Y", "2011-10-10T11:22:33+12:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 Z", "2011-10-10T11:22:33-00:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 +0000", "2011-10-10T11:22:33+00:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 -0000", "2011-10-10T11:22:33-00:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 +0100", "2011-10-10T11:22:33+01:00"),
            new Test("Mon, 10 Oct 2011 11:22:33 -0100", "2011-10-10T11:22:33-01:00")
        };

        [Fact]
        public void TestParseDates()
        {
            foreach (var test in _tests)
            {
                var json = string.Format("{{ date : new Date('{0}') }}", test.Json);
                BsonDocument document = null;
                try
                {
                    document = BsonDocument.Parse(json);
                }
                catch (Exception ex)
                {
                    var message = string.Format("Error parsing: new Date(\"{0}\"). Message: {1}.", test.Json, ex.Message);
                    throw new AssertionException(message); // note: the test data for 2-digit years needs to be adjusted at the beginning of each year
                }
                var dateTime = document["date"].ToUniversalTime();
                var expected = DateTime.Parse(test.Iso).ToUniversalTime();
                Assert.Equal(DateTimeKind.Utc, dateTime.Kind);
                Assert.Equal(DateTimeKind.Utc, expected.Kind);
                if (dateTime != expected)
                {
                    var message = string.Format("Parsing new Date(\"{0}\") did not yield expected result {1}.", test.Json, expected.ToString("o"));
                    throw new AssertionException(message);
                }
            }
        }
    }
}
