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
using System.Threading;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class ConventionRunnerTests
    {
        private ConventionPack _pack;
        private ConventionRunner _subject;

        public ConventionRunnerTests()
        {
            int orderIndex = 0;

            _pack = new ConventionPack();
            _pack.AddRange(new IConvention[]
            {
                new TrackingBeforeConvention(GetRunOrderIndex) { Name = "1" },
                new TrackingMemberConvention(GetRunOrderIndex) { Name = "3" },
                new TrackingAfterConvention(GetRunOrderIndex) { Name = "5" },
                new TrackingMemberConvention(GetRunOrderIndex) { Name = "4" },
                new TrackingAfterConvention(GetRunOrderIndex) { Name = "6" },
                new TrackingBeforeConvention(GetRunOrderIndex) { Name = "2" },
            });
            _subject = new ConventionRunner(_pack);

            var classMap = new BsonClassMap<TestClass>(cm =>
            {
                cm.MapMember(t => t.Prop1);
                cm.MapMember(t => t.Prop2);
            });

            _subject.Apply(classMap);

            int GetRunOrderIndex() => Interlocked.Increment(ref orderIndex);
        }

        [Fact]
        public void TestThatItRunsAllConventions()
        {
            var allRun = _pack.Conventions.OfType<ITrackRun>().All(x => x.IsRun);
            Assert.True(allRun);
        }

        [Fact]
        public void TestThatItRunsConventionsInTheProperOrder()
        {
            var conventions = _pack.Conventions.OfType<ITrackRun>().OrderBy(x => x.RunOrder).ToList();
            for (int i = 1; i < conventions.Count; i++)
            {
                if (conventions[i - 1].Name != i.ToString())
                {
                    var message = string.Format("Convention ran out of order. Expected {0} but was {1}.", conventions[0].Name, i);
                    throw new AssertionException(message);
                }
            }
        }

        [Fact]
        public void TestThatItRunsClassMapConventionsOnceEach()
        {
            var beforeConventions = _pack.Conventions.OfType<IClassMapConvention>().OfType<ITrackRun>();
            var allAreRunOnce = beforeConventions.All(x => x.RunCount == 1);

            Assert.True(allAreRunOnce);
        }

        [Fact]
        public void TestThatItRunsPostProcessingConventionsOnceEach()
        {
            var afterConventions = _pack.Conventions.OfType<IPostProcessingConvention>().OfType<ITrackRun>();
            var allAreRunOnce = afterConventions.All(x => x.RunCount == 1);

            Assert.True(allAreRunOnce);
        }

        [Fact]
        public void TestThatItRunsMemberConventionsTwiceEach()
        {
            var memberConventions = _pack.Conventions.OfType<IMemberMapConvention>().OfType<ITrackRun>();
            var allAreRunTwice = memberConventions.All(x => x.RunCount == 2);

            Assert.True(allAreRunTwice);
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

            long RunOrder { get; }
        }

        private class TrackingBeforeConvention : IClassMapConvention, ITrackRun
        {
            private readonly Func<int> _orderIndexProvider;

            public TrackingBeforeConvention(Func<int> orderIndexProvider)
            {
                _orderIndexProvider = orderIndexProvider;
            }

            public bool IsRun { get; set; }

            public int RunCount { get; set; }

            public long RunOrder { get; set; }

            public string Name { get; set; }

            public void Apply(BsonClassMap classMap)
            {
                IsRun = true;
                RunCount++;
                RunOrder = _orderIndexProvider();
            }
        }

        private class TrackingMemberConvention : IMemberMapConvention, ITrackRun
        {
            private readonly Func<int> _orderIndexProvider;

            public TrackingMemberConvention(Func<int> orderIndexProvider)
            {
                _orderIndexProvider = orderIndexProvider;
            }

            public bool IsRun { get; set; }

            public int RunCount { get; set; }

            public long RunOrder { get; set; }

            public string Name { get; set; }

            public void Apply(BsonMemberMap memberMap)
            {
                IsRun = true;
                RunCount++;
                RunOrder = _orderIndexProvider();
            }

            public void Apply(BsonMemberMap memberMap, IBsonSerializationDomain domain)
            {
                throw new NotImplementedException();
            }
        }

        private class TrackingAfterConvention : IPostProcessingConvention, ITrackRun
        {
            private readonly Func<int> _orderIndexProvider;

            public TrackingAfterConvention(Func<int> orderIndexProvider)
            {
                _orderIndexProvider = orderIndexProvider;
            }

            public bool IsRun { get; set; }

            public int RunCount { get; set; }

            public long RunOrder { get; set; }

            public string Name { get; set; }

            public void PostProcess(BsonClassMap classMap)
            {
                IsRun = true;
                RunCount++;
                RunOrder = _orderIndexProvider();
            }

            public void PostProcess(BsonClassMap classMap, IBsonSerializationDomain domain)
            {
                throw new NotImplementedException();
            }
        }
    }
}
