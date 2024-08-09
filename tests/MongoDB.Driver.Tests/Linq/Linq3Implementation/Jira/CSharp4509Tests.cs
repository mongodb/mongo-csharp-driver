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
using System.Linq.Expressions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4509Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(JobSortColumn.DriverName, "DriverName")]
        [InlineData(JobSortColumn.JobNumber, "JobNumber")]
        public void OrderByDescending_should_work(
            JobSortColumn sortOrder,
            string expectedSortField)
        {
            var collection = CreateCollection();

            Expression<Func<DbJob, object>> selector = sortOrder switch
            {
                JobSortColumn.JobNumber => j => j.JobNumber,
                JobSortColumn.DriverName => j => j.DriverName,
                _ => throw new NotSupportedException()
            };

            var queryable =
                collection.AsQueryable()
                .OrderByDescending(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, $"{{ $sort : {{ {expectedSortField} : -1 }} }}");
        }

        private IMongoCollection<DbJob> CreateCollection()
        {
            var collection = GetCollection<DbJob>("jobs");
            return collection;
        }

        public class DbJob
        {
            public int JobNumber { get; set; }
            public string DriverName { get; set; }
        }

        public enum JobSortColumn { JobNumber, DriverName }
    }
}
