/* Copyright 2013-2014 MongoDB Inc.
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

using System.Security;
using FluentAssertions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Authentication
{
    [TestFixture]
    public class AuthenticationHelperTests
    {
        [Test]
        [TestCase("user", "pencil", "1c33006ec1ffd90f9cadcbcc0e118200")]
        public void MongoPasswordDigest_should_create_the_correct_hash(string username, string password, string expected)
        {
            var securePassword = new SecureString();
            foreach (var c in password)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();

            var passwordDigest = AuthenticationHelper.MongoPasswordDigest(username, securePassword);

            passwordDigest.Should().BeEquivalentTo(expected);
        }
    }
}
