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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4493Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Find_with_predicate_should_work()
        {
            var collection = CreateCollection();
            string[] emails = { "email4@test.net" };
            string[] addresses = { "495 pacific street plymouth, ma 02360" };
            Expression<Func<Customer, bool>> filterByAddressAndEmails =
                c => c.Emails.Any(ce => emails.Contains(ce.Email.ToLower())) &&
                     addresses.Contains(c.Address.FullAddress.ToLower());
            FilterDefinition<Customer> emptyPredicate = Builders<Customer>.Filter.Empty;

            var find = collection.Find(filterByAddressAndEmails & emptyPredicate);

            var translatedFilter = TranslateFindFilter(collection, find);
            translatedFilter.Should().Be("{$and: [{$expr: {$anyElementTrue: {$map: {input: '$Emails', as: 'ce', in: {$in: [{$toLower: '$$ce.Email'}, ['email4@test.net']]}}}}}, {$expr: {$in: [{$toLower: '$Address.FullAddress'}, ['495 pacific street plymouth, ma 02360']]}}]}");

            var results = find.ToList();
            Assert.Equal(1, results.Count);
            results.Select(x => x.Id).Should().Equal(2);
        }

        private IMongoCollection<Customer> CreateCollection()
        {
            var collection = GetCollection<Customer>("C");

            CreateCollection(
                collection,
                new Customer {
                    Id = 1,
                    Address = new AddressMetadata
                    {
                        FullAddress = "111 atlantic street plymouth, ma 02345"
                    },
                    Emails = new[]
                    {
                        new EmailMetadata
                        {
                            Email = "email4@test.net"
                        }
                    }
                },
                new Customer
                {
                    Id = 2,
                    Address = new AddressMetadata
                    {
                        FullAddress = "495 pacific street plymouth, ma 02360"
                    },
                    Emails = new[]
                    {
                        new EmailMetadata
                        {
                            Email = "email4@test.net"
                        }
                    }
                },
                new Customer
                {
                    Id = 3,
                    Address = new AddressMetadata
                    {
                        FullAddress = "495 pacific street plymouth, ma 02360"
                    },
                    Emails = new[]
                    {
                        new EmailMetadata
                        {
                            Email = "notEmail4@test.net"
                        }
                    }
                });

            return collection;
        }

        public class Customer
        {
            public int Id { get; set; }
            public AddressMetadata Address { get; set; }
            public IList<EmailMetadata> Emails { get; set; }
        }

        public sealed record AddressMetadata
        {
            public string FullAddress { get; set; }
        }

        public sealed record EmailMetadata
        {
            public string Email { get; set; }
        }
    }
}
