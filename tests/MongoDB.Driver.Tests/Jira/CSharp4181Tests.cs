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
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3ImplementationTests;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp4181Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(LinqProvider.V2)]
        [InlineData(LinqProvider.V3)]
        public void Test(LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var aggregate = collection
                .Aggregate()
                .Project(x => new ProjectResult { Payment = x.Payments == null ? null : x.Payments.First(y => y.Date == x.Payments.Max(y => y.Date)) });

            var stages = Translate(collection, aggregate);
            // note: bug exists in LINQ2 translation but works correctly in LINQ3
            var expectedStage =
                linqProvider == LinqProvider.V2 ?
                    "{ $project : { Payment : { $cond : [{ $eq : ['$Payments', null] }, null, { $arrayElemAt : [{ $filter : { input : '$Payments', as :'y', cond : { $eq : ['$$y.Date', { $max : '$$y.Payments.Date' }] } } }, 0] }] }, _id :0 } }" :
                    "{ $project : { Payment : { $cond : { if : { $eq : ['$Payments', null] }, then : null, else : { $arrayElemAt : [{ $filter : { input : '$Payments', as : 'y', cond : { $eq : ['$$y.Date', { $max : '$Payments.Date' }] } } }, 0] } } }, _id : 0 } }";
            AssertStages(stages, expectedStage);
        }

        private IMongoCollection<Remittance> CreateCollection(LinqProvider linqProvider)
        {
            var client = linqProvider == LinqProvider.V2 ? DriverTestConfiguration.Client : DriverTestConfiguration.Linq3Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<Remittance>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            var documents = new[]
            {
                new Remittance
                {
                    Id = 1,
                    Payments = new[]
                    {
                        new Payment { Date = DateTime.Parse("2020-06-01 04:22:59.948Z") },
                        new Payment { Date = DateTime.Parse("2020-06-01 05:22:59.948Z") },
                        new Payment { Date = DateTime.Parse("2020-06-01 06:22:59.948Z") },
                    }
                }
            };
            CreateCollection(collection, documents);

            return collection;
        }

        public class Remittance
        {
            public int Id { get; set; }
            public Payment[] Payments { get; set; }
        }

        public class Payment
        {
            public DateTime Date { get; set; }
        }

        public class ProjectResult
        {
            public Payment Payment { get; set; }
        }
    }
}
