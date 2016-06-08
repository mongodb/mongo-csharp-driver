/* Copyright 2010-2015 MongoDB Inc.
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

using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoServerBuildInfoTests
    {
        [Fact]
        public void TestVersion2_4()
        {
            var buildInfo = new MongoServerBuildInfo("2.4");
            Assert.Equal(2, buildInfo.Version.Major);
            Assert.Equal(4, buildInfo.Version.Minor);
            Assert.Equal(-1, buildInfo.Version.Build);
            Assert.Equal(-1, buildInfo.Version.Revision);
        }

        [Fact]
        public void TestVersion2_4_0()
        {
            var buildInfo = new MongoServerBuildInfo("2.4.0");
            Assert.Equal(2, buildInfo.Version.Major);
            Assert.Equal(4, buildInfo.Version.Minor);
            Assert.Equal(0, buildInfo.Version.Build);
            Assert.Equal(-1, buildInfo.Version.Revision);
        }

        [Fact]
        public void TestVersion2_4_1()
        {
            var buildInfo = new MongoServerBuildInfo("2.4.1");
            Assert.Equal(2, buildInfo.Version.Major);
            Assert.Equal(4, buildInfo.Version.Minor);
            Assert.Equal(1, buildInfo.Version.Build);
            Assert.Equal(-1, buildInfo.Version.Revision);
        }

        [Fact]
        public void TestVersion2_4_1_rc0()
        {
            var buildInfo = new MongoServerBuildInfo("2.4.1-rc0");
            Assert.Equal(2, buildInfo.Version.Major);
            Assert.Equal(4, buildInfo.Version.Minor);
            Assert.Equal(1, buildInfo.Version.Build);
            Assert.Equal(-1, buildInfo.Version.Revision);
        }

        [Fact]
        public void TestVersion2_4_1_2()
        {
            var buildInfo = new MongoServerBuildInfo("2.4.1.2");
            Assert.Equal(2, buildInfo.Version.Major);
            Assert.Equal(4, buildInfo.Version.Minor);
            Assert.Equal(1, buildInfo.Version.Build);
            Assert.Equal(2, buildInfo.Version.Revision);
        }

        [Fact]
        public void TestVersion2_4_1_2_rc0()
        {
            var buildInfo = new MongoServerBuildInfo("2.4.1.2-rc0");
            Assert.Equal(2, buildInfo.Version.Major);
            Assert.Equal(4, buildInfo.Version.Minor);
            Assert.Equal(1, buildInfo.Version.Build);
            Assert.Equal(2, buildInfo.Version.Revision);
        }

        [Fact]
        public void TestVersion2_4_dot_beta()
        {
            var buildInfo = new MongoServerBuildInfo("2.4.beta");
            Assert.Equal(2, buildInfo.Version.Major);
            Assert.Equal(4, buildInfo.Version.Minor);
            Assert.Equal(-1, buildInfo.Version.Build);
            Assert.Equal(-1, buildInfo.Version.Revision);
        }

        [Fact]
        public void TestVersion2_4_alpha1()
        {
            var buildInfo = new MongoServerBuildInfo("2.4a1");
            Assert.Equal(2, buildInfo.Version.Major);
            Assert.Equal(4, buildInfo.Version.Minor);
            Assert.Equal(-1, buildInfo.Version.Build);
            Assert.Equal(-1, buildInfo.Version.Revision);
        }

        [Fact]
        public void TestVersionInvalid()
        {
            var buildInfo = new MongoServerBuildInfo("v2.4a1");
            Assert.Equal(0, buildInfo.Version.Major);
            Assert.Equal(0, buildInfo.Version.Minor);
            Assert.Equal(0, buildInfo.Version.Build);
            Assert.Equal(-1, buildInfo.Version.Revision);
        }
    }
}
