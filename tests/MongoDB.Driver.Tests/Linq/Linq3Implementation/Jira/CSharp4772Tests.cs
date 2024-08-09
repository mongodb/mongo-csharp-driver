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
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4772Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Select_with_Any_should_work()
        {
            var collection = GetCollection();
            var organizationId = 1;

            var queryable = collection.AsQueryable()
                .Select(a =>
                    a.ProfilesList != null &&
                    a.ProfilesList.Any(p => p.OrganizationIdsList.Any(o => o == organizationId)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $and : [{ $ne : ['$ProfilesList', null] }, { $anyElementTrue : { $map : { input : '$ProfilesList', as : 'p', in : { $anyElementTrue : { $map : { input : '$$p.OrganizationIdsList', as : 'o', in : { $eq : ['$$o', 1] } } } } } }  }] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, false, false, false);
        }

        [Fact]
        public void Select_with_Array_Exists_should_work()
        {
            var collection = GetCollection();
            var organizationId = 1;

            var queryable = collection.AsQueryable()
                .Select(a =>
                    a.ProfilesArray != null &&
                    Array.Exists(a.ProfilesArray, p => Array.Exists(p.OrganizationIdsArray, o => o == organizationId)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $and : [{ $ne : ['$ProfilesArray', null] }, { $anyElementTrue : { $map : { input : '$ProfilesArray', as : 'p', in : { $anyElementTrue : { $map : { input : '$$p.OrganizationIdsArray', as : 'o', in : { $eq : ['$$o', 1] } } } } } }  }] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, false, false, false);
        }

        [Fact]
        public void Select_with_List_Exists_should_work()
        {
            var collection = GetCollection();
            var organizationId = 1;

            var queryable = collection.AsQueryable()
                .Select(a =>
                    a.ProfilesList != null &&
                    a.ProfilesList.Exists(p => p.OrganizationIdsList.Exists(o => o == organizationId)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $and : [{ $ne : ['$ProfilesList', null] }, { $anyElementTrue : { $map : { input : '$ProfilesList', as : 'p', in : { $anyElementTrue : { $map : { input : '$$p.OrganizationIdsList', as : 'o', in : { $eq : ['$$o', 1] } } } } } }  }] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(true, false, false, false);
        }

        [Fact]
        public void Where_with_Any_should_work()
        {
            var collection = GetCollection();
            var organizationId = 1;

            var queryable = collection.AsQueryable()
                .Where(a =>
                    a.ProfilesList != null &&
                    a.ProfilesList.Any(p => p.OrganizationIdsList.Any(o => o == organizationId)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { ProfilesList : { $ne : null, $elemMatch : { OrganizationIdsList : 1 } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_with_Array_Exists_should_work()
        {
            var collection = GetCollection();
            var organizationId = 1;

            var queryable = collection.AsQueryable()
                .Where(a =>
                    a.ProfilesArray != null &&
                    Array.Exists(a.ProfilesArray, p => Array.Exists(p.OrganizationIdsArray, o => o == organizationId)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { ProfilesArray : { $ne : null, $elemMatch : { OrganizationIdsArray : 1 } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_with_List_Exists_should_work()
        {
            var collection = GetCollection();
            var organizationId = 1;

            var queryable = collection.AsQueryable()
                .Where(a =>
                    a.ProfilesList != null &&
                    a.ProfilesList.Exists(p => p.OrganizationIdsList.Exists(o => o == organizationId)));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { ProfilesList : { $ne : null, $elemMatch : { OrganizationIdsList : 1 } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        private IMongoCollection<Account> GetCollection()
        {
            var collection = GetCollection<Account>("test");
            CreateCollection(
                collection,
                new Account
                {
                    Id = 1,
                    ProfilesArray = new Profile[] { new Profile { OrganizationIdsArray = new int[] { 1 } } },
                    ProfilesList = new List<Profile> { new Profile { OrganizationIdsList = new List<int> { 1 } } }
                },
                new Account
                {
                    Id = 2,
                    ProfilesArray = new Profile[] { new Profile { OrganizationIdsArray = new int[] { 2 } } },
                    ProfilesList = new List<Profile> { new Profile { OrganizationIdsList = new List<int> { 2 } } }
                },
                new Account
                {
                    Id = 3,
                    ProfilesArray = new Profile[0],
                    ProfilesList = new List<Profile>()
                },
                new Account
                {
                    Id = 4,
                    ProfilesArray = new Profile[] { new Profile { OrganizationIdsArray = new int[0] } },
                    ProfilesList = new List<Profile> { new Profile { OrganizationIdsList = new List<int>() } }
                });
            return collection;
        }

        private class Account
        {
            public int Id { get; set; }
            public Profile[] ProfilesArray { get; set; }
            public List<Profile> ProfilesList { get; set; }
        }

        private class Profile
        {
            public int[] OrganizationIdsArray { get; set; }
            public List<int> OrganizationIdsList { get; set; }
        }
    }
}
