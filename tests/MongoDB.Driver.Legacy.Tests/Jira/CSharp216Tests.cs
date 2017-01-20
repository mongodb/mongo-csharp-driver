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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests.Jira.CSharp216
{
    public class CSharp216Tests
    {
        private MongoDatabase _adminDatabase;

        public CSharp216Tests()
        {
            _adminDatabase = LegacyTestConfiguration.Server.GetDatabase("admin");
        }

        [Fact]
        public void TestAmbiguousEvalArguments()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                var code = "function (x, y) { return y; }";
                var objectArrayArg = new object[] { 1, 2, 3 };
                var boolArg = true;
#pragma warning disable 618
                var result = _adminDatabase.Eval(code, objectArrayArg, boolArg); // before change boolArg was being misinterpreted as nolock argument
#pragma warning restore
                Assert.Equal(BsonType.Boolean, result.BsonType);
                Assert.Equal(true, result.AsBoolean);
            }
        }

        [Fact]
        public void TestNoLock()
        {
            if (!DriverTestConfiguration.Client.Settings.Credentials.Any())
            {
                var code = "function (x, y) { return y; }";
                var objectArrayArg = new object[] { 1, 2, 3 };
                var boolArg = true;
#pragma warning disable 618
                var result = _adminDatabase.Eval(EvalFlags.NoLock, code, objectArrayArg, boolArg); // before change boolArg was being misinterpreted as nolock argument
#pragma warning restore
                Assert.Equal(BsonType.Boolean, result.BsonType);
                Assert.Equal(true, result.AsBoolean);
            }
        }
    }
}
