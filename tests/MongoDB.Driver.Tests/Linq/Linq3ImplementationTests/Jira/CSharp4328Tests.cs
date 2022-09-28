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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4328Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("USA")]
        public void Filter_using_First_should_work(string country)
        {
            var collection = CreateCollection();
            var startTargetDeliveryDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endTargetDeliveryDate = new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            var filterBuilder = Builders<SubscriptionRepositorySubscriptionModel>.Filter;
            var statusFilter = filterBuilder.Where(x => x.SubscriptionStatus == SubscriptionStatus.Active);
            var dateFilter = filterBuilder.Where(
                x =>
                    x.UpcomingOrders.First().NextTargetDeliveryDate >= startTargetDeliveryDate &&
                    x.UpcomingOrders.First().NextTargetDeliveryDate <= endTargetDeliveryDate);
            var filter = filterBuilder.And(statusFilter, dateFilter);
            if (!string.IsNullOrEmpty(country))
            {
                filter = filterBuilder.And(filter, filterBuilder.Where(x => x.ShippingAddress.CountryCode == country));
            }

            var renderedFilter = Translate(collection, filter);
            var expectedFilter = BsonDocument.Parse(
                @"
                {
                    SubscriptionStatus : 1,
                    'UpcomingOrders.0.NextTargetDeliveryDate' : {
                        $gte : ISODate('2022-01-01T00:00:00Z'),
                        $lte : ISODate('2022-12-31T00:00:00Z')
                    }
                }");
            if (!string.IsNullOrEmpty(country))
            {
                expectedFilter["ShippingAddress.CountryCode"] = country;
            }
            renderedFilter.Should().Be(expectedFilter);

            var result = collection.Distinct(x => x.CustomerId, filter).ToList();
            result.Should().Equal(2);
        }

        private IMongoCollection<SubscriptionRepositorySubscriptionModel> CreateCollection()
        {
            var collection = GetCollection<SubscriptionRepositorySubscriptionModel>();

            var documents = new SubscriptionRepositorySubscriptionModel[]
            {
                new SubscriptionRepositorySubscriptionModel
                {
                    Id = 1,
                    CustomerId = 1,
                    SubscriptionStatus = SubscriptionStatus.Inactive,
                    UpcomingOrders = new Order[0],
                    ShippingAddress = null
                },
                new SubscriptionRepositorySubscriptionModel
                {
                    Id = 2,
                    CustomerId = 2,
                    SubscriptionStatus = SubscriptionStatus.Active,
                    UpcomingOrders = new Order[] { new Order { NextTargetDeliveryDate = new DateTime(2022, 12, 1, 0, 0, 0, DateTimeKind.Utc) } },
                    ShippingAddress = new Address { CountryCode = "USA" }
                }

            };
            CreateCollection(collection, documents);

            return collection;
        }

        private class SubscriptionRepositorySubscriptionModel
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public SubscriptionStatus SubscriptionStatus { get; set; }
            public Order[] UpcomingOrders { get; set; }
            public Address ShippingAddress { get; set; }
        }

        private class Order
        {
            public DateTime NextTargetDeliveryDate { get; set; }
        }

        private class Address
        {
            public string CountryCode { get; set; }
        }

        private enum SubscriptionStatus
        {
            Inactive = 0,
            Active = 1
        }
    }
}
