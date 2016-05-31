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
#pragma warning disable 618
    public class MongoUserTests
    {
        [Fact]
        public void TestConstructor1()
        {
            var u = new MongoUser("u", new PasswordEvidence("p"), true);
            var ph = MongoUser.HashPassword("u", "p");
            Assert.Equal("u", u.Username);
            Assert.Equal(ph, u.PasswordHash);
            Assert.Equal(true, u.IsReadOnly);
        }

        [Fact]
        public void TestConstructor2()
        {
            var h = MongoUser.HashPassword("u", "p");
            var u = new MongoUser("u", h, true);
            Assert.Equal("u", u.Username);
            Assert.Equal(h, u.PasswordHash);
            Assert.Equal(true, u.IsReadOnly);
        }

        [Fact]
        public void TestEquals()
        {
            var a1 = new MongoUser("u", "h", false);
            var a2 = new MongoUser("u", "h", false);
            var a3 = a2;
            var b = new MongoUser("x", "h", false);
            var c = new MongoUser("u", "x", false);
            var d = new MongoUser("u", "h", true);
            var null1 = (MongoUser)null;
            var null2 = (MongoUser)null;

            Assert.NotSame(a1, a2);
            Assert.Same(a2, a3);
            Assert.True(a1.Equals((object)a2));
            Assert.False(a1.Equals((object)null));
            Assert.False(a1.Equals((object)"x"));

            Assert.True(a1 == a2);
            Assert.True(a2 == a3);
            Assert.False(a1 == b);
            Assert.False(a1 == c);
            Assert.False(a1 == d);
            Assert.False(a1 == null1);
            Assert.False(null1 == a1);
            Assert.True(null1 == null2);

            Assert.False(a1 != a2);
            Assert.False(a2 != a3);
            Assert.True(a1 != b);
            Assert.True(a1 != c);
            Assert.True(a1 != d);
            Assert.True(a1 != null1);
            Assert.True(null1 != a1);
            Assert.False(null1 != null2);

            Assert.Equal(a1.GetHashCode(), a2.GetHashCode());
        }
    }
#pragma warning restore
}
