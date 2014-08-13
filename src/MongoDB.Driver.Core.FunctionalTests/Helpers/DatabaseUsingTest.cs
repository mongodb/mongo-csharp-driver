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

using NUnit.Framework;

namespace MongoDB.Driver.Core.FunctionalTests.Helpers
{
    public abstract class DatabaseUsingTest : ClusterUsingTest
    {
        // fields
        protected string _databaseName;

        // properties
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        // methods
        protected virtual void DropDatabase()
        {
            // override if you created a database just for this test
        }

        protected virtual string GetDatabaseName()
        {
            // override if you need a separate database just for this test
            return SuiteConfiguration.DatabaseName;
        }

        [TestFixtureSetUp]
        public void DatabaseUsingTestSetUp()
        {
            _databaseName = GetDatabaseName();
        }

        [TestFixtureTearDown]
        public void DatabaseUsingTestTearDown()
        {
            DropDatabase();
        }
    }
}
