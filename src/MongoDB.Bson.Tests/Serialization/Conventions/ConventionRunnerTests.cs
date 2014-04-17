/* Copyright 2010-2014 MongoDB Inc.
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

using System.Diagnostics;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Serialization.Conventions
{
    [TestFixture]
    public class ConventionRunnerTests
    {
        private ConventionPack _pack;
        private ConventionRunner _subject;

        [SetUp]
        public void SetUp()
        {
            var stopwatch = new Stopwatch();
            _pack = new ConventionPack();
            _pack.AddRange(new IConvention[] 
            {
                new TrackingBeforeConvention(stopwatch) { Name = "1" },
                new TrackingMemberConvention(stopwatch) { Name = "3" },
                new TrackingAfterConvention(stopwatch) { Name = "5" },
                new TrackingMemberConvention(stopwatch) { Name = "4" },
                new TrackingAfterConvention(stopwatch) { Name = "6" },
                new TrackingBeforeConvention(stopwatch) { Name = "2" },
            });
            _subject = new ConventionRunner(_pack);

            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.MapMember(t => t.Prop1);
                cm.MapMember(t => t.Prop2);
            });

            stopwatch.Start();
            _subject.Apply(classMap);
            stopwatch.Stop();
        }

        [Test]
        public void TestThatItRunsAllConventions()
        {
            var allRun = _pack.Conventions.OfType<ITrackRun>().All(x => x.IsRun);
            Assert.IsTrue(allRun);
        }

        [Test]
        public void TestThatItRunsConventionsInTheProperOrder()
        {
            var conventions = _pack.Conventions.OfType<ITrackRun>().OrderBy(x => x.RunTicks).ToList();
            for (int i = 1; i < conventions.Count; i++)
            {
                if (conventions[i - 1].Name != i.ToString())
                {
                    Assert.Fail("Convention ran out of order. Expected {0} but was {1}.", conventions[0].Name, i);
                }
            }
        }

        [Test]
        public void TestThatItRunsClassMapConventionsOnceEach()
        {
            var beforeConventions = _pack.Conventions.OfType<IClassMapConvention>().OfType<ITrackRun>();
            var allAreRunOnce = beforeConventions.All(x => x.RunCount == 1);

            Assert.IsTrue(allAreRunOnce);
        }

        [Test]
        public void TestThatItRunsPostProcessingConventionsOnceEach()
        {
            var afterConventions = _pack.Conventions.OfType<IPostProcessingConvention>().OfType<ITrackRun>();
            var allAreRunOnce = afterConventions.All(x => x.RunCount == 1);

            Assert.IsTrue(allAreRunOnce);
        }

        [Test]
        public void TestThatItRunsMemberConventionsTwiceEach()
        {
            var memberConventions = _pack.Conventions.OfType<IMemberMapConvention>().OfType<ITrackRun>();
            var allAreRunTwice = memberConventions.All(x => x.RunCount == 2);

            Assert.IsTrue(allAreRunTwice);
        }

        private class TestClass
        {
            public string Prop1 { get; set; }

            public string Prop2 { get; set; }
        }

        private interface ITrackRun
        {
            bool IsRun { get; }

            string Name { get; }

            int RunCount { get; }

            long RunTicks { get; }
        }

        private class TrackingBeforeConvention : IClassMapConvention, ITrackRun
        {
            private readonly Stopwatch _stopwatch;

            public TrackingBeforeConvention(Stopwatch stopwatch)
            {
                _stopwatch = stopwatch;
            }

            public bool IsRun { get; set; }

            public int RunCount { get; set; }

            public long RunTicks { get; set; }

            public string Name { get; set; }

            public void Apply(BsonClassMap classMap)
            {
                IsRun = true;
                RunCount++;
                RunTicks = _stopwatch.ElapsedTicks;
            }
        }

        private class TrackingMemberConvention : IMemberMapConvention, ITrackRun
        {
            private readonly Stopwatch _stopwatch;

            public TrackingMemberConvention(Stopwatch stopwatch)
            {
                _stopwatch = stopwatch;
            }

            public bool IsRun { get; set; }

            public int RunCount { get; set; }

            public long RunTicks { get; set; }

            public string Name { get; set; }

            public void Apply(BsonMemberMap memberMap)
            {
                IsRun = true;
                RunCount++;
                RunTicks = _stopwatch.ElapsedTicks;
            }
        }

        private class TrackingAfterConvention : IPostProcessingConvention, ITrackRun
        {
            private readonly Stopwatch _stopwatch;

            public TrackingAfterConvention(Stopwatch stopwatch)
            {
                _stopwatch = stopwatch;
            }

            public bool IsRun { get; set; }

            public int RunCount { get; set; }

            public long RunTicks { get; set; }

            public string Name { get; set; }

            public void PostProcess(BsonClassMap classMap)
            {
                IsRun = true;
                RunCount++;
                RunTicks = _stopwatch.ElapsedTicks;
            }
        }
    }
}