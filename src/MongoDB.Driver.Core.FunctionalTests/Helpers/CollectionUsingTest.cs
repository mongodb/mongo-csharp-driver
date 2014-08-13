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

using System.Linq;
using NUnit.Framework;

namespace MongoDB.Driver.Core.FunctionalTests.Helpers
{
    public abstract class CollectionUsingTest : DatabaseUsingTest
    {
        // fields
        private string _collectionName;

        // properties
        public string CollectionName
        {
            get { return _collectionName; }
        }

        // methods
        protected virtual void CreateCollection()
        {
            // override if you need to create the collection a certain way
        }

        protected virtual void DropCollection()
        {
            // TODO: implement DropCollection
        }

        protected virtual string GetCollectionName()
        {
            var type = GetType();
            var specificationFolder = type.Namespace.Split('.').Last();
            var specificationName = type.Name;
            return string.Format("{0}_{1}", specificationFolder, specificationName);
        }

        [TestFixtureSetUp]
        public void CollectionUsingTestSetUp()
        {
            _collectionName = GetCollectionName();
            CreateCollection();
        }

        [TestFixtureTearDown]
        public void CollectionUsingTestTearDown()
        {
            DropCollection();
        }
    }
}
