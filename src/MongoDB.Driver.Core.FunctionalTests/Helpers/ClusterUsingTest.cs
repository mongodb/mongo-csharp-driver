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

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Core.FunctionalTests.Helpers
{
    public abstract class ClusterUsingTest
    {
        // fields
        protected ICluster _cluster;

        // methods
        protected Exception Catch(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        [TestFixtureSetUp]
        public void ClusterUsingTestSetUp()
        {
            _cluster = CreateCluster();
        }

        [TestFixtureTearDown]
        public void ClusterUsingTestTearDown()
        {
            DisposeCluster();
        }

        protected virtual ICluster CreateCluster()
        {
            // override if you want to use a new cluster just for this test
            return SuiteConfiguration.Cluster;
        }

        protected virtual void DisposeCluster()
        {
            // override if you overrode CreateCluster
        }

        protected Task<TResult> ExecuteOperationAsync<TResult>(IWriteOperation<TResult> operation, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var binding = new CurrentPrimaryBinding(_cluster))
            {
                return operation.ExecuteAsync(binding, timeout, cancellationToken);
            }
        }
    }
}
