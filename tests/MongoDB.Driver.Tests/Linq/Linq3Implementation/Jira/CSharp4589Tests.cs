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

using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4589Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Multiple_GroupJoins_using_method_syntax_should_work()
        {
            var teams = CreateTeamsCollection();
            var teamAllianceMappings = CreateTeamAllianceMappingsCollection();
            var organizationAdmins = CreateOrganizationAdminsCollection();
            var users = CreateUsersCollection();

            // this is the LINQ query syntax provided by the user in the JIRA ticket

            //var queryable =
            //    from team in teams.AsQueryable()
            //    join teamAllianceMappingList in teamAllianceMappings.AsQueryable()
            //        on team.TeamId equals teamAllianceMappingList.TeamId
            //        into teamAllianceMappingsListTemp
            //    from allianceMapping in teamAllianceMappingsListTemp.DefaultIfEmpty()
            //    join organizationAdminsList in organizationAdmins.AsQueryable()
            //        on team.OrganizationId equals organizationAdminsList.OrganizationId
            //        into organizationAdminsListTemp
            //    from organizationAdmin in organizationAdminsListTemp.DefaultIfEmpty()
            //    join usersList in users.AsQueryable()
            //        on organizationAdmin.UserId equals usersList.UserId
            //        into usersListTemp
            //    from organizationUser in usersListTemp.DefaultIfEmpty()
            //    select new
            //    {
            //        TeamId = team.TeamId,
            //        TeamName = team.TeamName,
            //        OrganizationLogo = (organizationUser != null ? organizationUser.ProfileImage : team.OrganizationLogo),
            //        AllianceTeamId = allianceMapping.AllianceTeamId
            //    };

            // this is the LINQ method syntax that the compiler translated the query syntax into (with some minor renamings so it will compile)
            // this is what the LINQ provider *actually* sees

            var queryable = teams.AsQueryable()
                .GroupJoin(
                    teamAllianceMappings.AsQueryable(),
                    team => team.TeamId,
                    teamAllianceMappingList => teamAllianceMappingList.TeamId,
                    (team, teamAllianceMappingsListTemp) => new { team = team, teamAllianceMappingsListTemp = teamAllianceMappingsListTemp })
                .SelectMany(
                    TransparentIdentifier0 => TransparentIdentifier0.teamAllianceMappingsListTemp.DefaultIfEmpty(),
                    (TransparentIdentifier0, allianceMapping) => new { TransparentIdentifier0 = TransparentIdentifier0, allianceMapping = allianceMapping })
                .GroupJoin(
                    organizationAdmins.AsQueryable(),
                    TransparentIdentifier1 => TransparentIdentifier1.TransparentIdentifier0.team.OrganizationId,
                    organizationAdminsList => organizationAdminsList.OrganizationId,
                    (TransparentIdentifier1, organizationAdminsListTemp) => new { TransparentIdentifier1 = TransparentIdentifier1, organizationAdminsListTemp = organizationAdminsListTemp })
                .SelectMany(
                    TransparentIdentifier2 => TransparentIdentifier2.organizationAdminsListTemp.DefaultIfEmpty(),
                    (TransparentIdentifier2, organizationAdmin) => new { TransparentIdentifier2 = TransparentIdentifier2, organizationAdmin = organizationAdmin })
                .GroupJoin(
                    users.AsQueryable(),
                    TransparentIdentifier3 => TransparentIdentifier3.organizationAdmin.UserId,
                    usersList => usersList.UserId,
                    (TransparentIdentifier3, usersListTemp) => new { TransparentIdentifier3 = TransparentIdentifier3, usersListTemp = usersListTemp })
                .SelectMany(
                    TransparentIdentifier4 => TransparentIdentifier4.usersListTemp.DefaultIfEmpty(),
                    (TransparentIdentifier4, organizationUser) =>
                        new
                        {
                            TeamId = TransparentIdentifier4.TransparentIdentifier3.TransparentIdentifier2.TransparentIdentifier1.TransparentIdentifier0.team.TeamId,
                            TeamName = TransparentIdentifier4.TransparentIdentifier3.TransparentIdentifier2.TransparentIdentifier1.TransparentIdentifier0.team.TeamName,
                            OrganizationLogo = (organizationUser != null ? organizationUser.ProfileImage : TransparentIdentifier4.TransparentIdentifier3.TransparentIdentifier2.TransparentIdentifier1.TransparentIdentifier0.team.OrganizationLogo),
                            AllianceTeamId = TransparentIdentifier4.TransparentIdentifier3.TransparentIdentifier2.TransparentIdentifier1.allianceMapping.AllianceTeamId
                        });

            var stages = Translate(teams, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'teamAllianceMappings', localField : '_outer.TeamId', foreignField : 'TeamId', as : '_inner' } }",
                "{ $project : { team : '$_outer', teamAllianceMappingsListTemp : '$_inner', _id : 0 } }",
                @"{ $project : {
                        _v: { $map : {
                            input : { $cond : {
                                if : { $eq : [{ $size : '$teamAllianceMappingsListTemp' }, 0] },
                                then : [null],
                                else : '$teamAllianceMappingsListTemp' } },
                            as : 'allianceMapping',
                            in : { TransparentIdentifier0 : '$$ROOT', allianceMapping : '$$allianceMapping' } } },
                        _id: 0 } }",
                "{ $unwind : '$_v' }",
                "{ $project : { _outer : '$_v', _id : 0 } }",
                "{ $lookup : { from : 'organizationAdmins', localField : '_outer.TransparentIdentifier0.team.OrganizationId', foreignField : 'OrganizationId', as : '_inner' } }",
                "{ $project : { TransparentIdentifier1 : '$_outer', organizationAdminsListTemp : '$_inner', _id : 0 } }",
                @"{ $project : {
                        _v : { $map : {
                            input : { $cond : {
                                if : { $eq : [{ $size : '$organizationAdminsListTemp' }, 0] },
                                then : [null],
                                else : '$organizationAdminsListTemp' } },
                            as : 'organizationAdmin',
                            in : { TransparentIdentifier2 : '$$ROOT', organizationAdmin : '$$organizationAdmin' } } },
                        _id : 0 } }",
                "{ $unwind : '$_v' }",
                "{ $project : { _outer : '$_v', _id : 0 } }",
                "{ $lookup : { from : 'users', localField : '_outer.organizationAdmin.UserId', foreignField : 'UserId', as : '_inner'  } }",
                "{ $project : { TransparentIdentifier3 : '$_outer', usersListTemp : '$_inner', _id : 0 } }",
                @"{ $project : {
                        _v : { $map : {
                            input : { $cond : {
                                if : { $eq : [{ $size : '$usersListTemp' }, 0] },
                                then : [null],
                                else : '$usersListTemp' } },
                            as : 'organizationUser',
                            in : {
                                TeamId : '$TransparentIdentifier3.TransparentIdentifier2.TransparentIdentifier1.TransparentIdentifier0.team.TeamId',
                                TeamName : '$TransparentIdentifier3.TransparentIdentifier2.TransparentIdentifier1.TransparentIdentifier0.team.TeamName',
                                OrganizationLogo : { $cond : {
                                    if : { $ne : ['$$organizationUser', null] },
                                    then : '$$organizationUser.ProfileImage',
                                    else : '$TransparentIdentifier3.TransparentIdentifier2.TransparentIdentifier1.TransparentIdentifier0.team.OrganizationLogo' } },
                                AllianceTeamId : '$TransparentIdentifier3.TransparentIdentifier2.TransparentIdentifier1.allianceMapping.AllianceTeamId' } } },
                        _id : 0 } }",
                "{ $unwind : '$_v' }");

            var results = queryable.ToList();
            var result = results.Single();
            result.TeamId.Should().Be(1);
            result.TeamName.Should().Be("team name");
            result.OrganizationLogo.Should().Be("profile image");
            result.AllianceTeamId.Should().Be(2);
        }

        [Fact]
        public void Multiple_GroupJoins_using_query_syntax_should_work()
        {
            var teams = CreateTeamsCollection();
            var teamAllianceMappings = CreateTeamAllianceMappingsCollection();
            var organizationAdmins = CreateOrganizationAdminsCollection();
            var users = CreateUsersCollection();

            var queryable =
                from team in teams.AsQueryable()
                join teamAllianceMappingList in teamAllianceMappings.AsQueryable()
                    on team.TeamId equals teamAllianceMappingList.TeamId
                    into teamAllianceMappingsListTemp
                from allianceMapping in teamAllianceMappingsListTemp.DefaultIfEmpty()
                join organizationAdminsList in organizationAdmins.AsQueryable()
                    on team.OrganizationId equals organizationAdminsList.OrganizationId
                    into organizationAdminsListTemp
                from organizationAdmin in organizationAdminsListTemp.DefaultIfEmpty()
                join usersList in users.AsQueryable()
                    on organizationAdmin.UserId equals usersList.UserId
                    into usersListTemp
                from organizationUser in usersListTemp.DefaultIfEmpty()
                select new
                {
                    TeamId = team.TeamId,
                    TeamName = team.TeamName,
                    OrganizationLogo = (organizationUser != null ? organizationUser.ProfileImage : team.OrganizationLogo),
                    AllianceTeamId = allianceMapping.AllianceTeamId
                };

            var stages = Translate(teams, queryable);
            AssertStages(
                stages,
                "{ $project : { _outer : '$$ROOT', _id : 0 } }",
                "{ $lookup : { from : 'teamAllianceMappings', localField : '_outer.TeamId', foreignField : 'TeamId', as : '_inner' } }",
                "{ $project : { team : '$_outer', teamAllianceMappingsListTemp : '$_inner', _id : 0 } }",
                @"{ $project : {
                        _v: { $map : {
                            input : { $cond : {
                                if : { $eq : [{ $size : '$teamAllianceMappingsListTemp' }, 0] },
                                then : [null],
                                else : '$teamAllianceMappingsListTemp' } },
                            as : 'allianceMapping',
                            in : { '<>h__TransparentIdentifier0' : '$$ROOT', allianceMapping : '$$allianceMapping' } } },
                        _id: 0 } }",
                "{ $unwind : '$_v' }",
                "{ $project : { _outer : '$_v', _id : 0 } }",
                "{ $lookup : { from : 'organizationAdmins', localField : '_outer.<>h__TransparentIdentifier0.team.OrganizationId', foreignField : 'OrganizationId', as : '_inner' } }",
                "{ $project : { '<>h__TransparentIdentifier1' : '$_outer', organizationAdminsListTemp : '$_inner', _id : 0 } }",
                @"{ $project : {
                        _v : { $map : {
                            input : { $cond : {
                                if : { $eq : [{ $size : '$organizationAdminsListTemp' }, 0] },
                                then : [null],
                                else : '$organizationAdminsListTemp' } },
                            as : 'organizationAdmin',
                            in : { '<>h__TransparentIdentifier2' : '$$ROOT', organizationAdmin : '$$organizationAdmin' } } },
                        _id : 0 } }",
                "{ $unwind : '$_v' }",
                "{ $project : { _outer : '$_v', _id : 0 } }",
                "{ $lookup : { from : 'users', localField : '_outer.organizationAdmin.UserId', foreignField : 'UserId', as : '_inner'  } }",
                "{ $project : { '<>h__TransparentIdentifier3' : '$_outer', usersListTemp : '$_inner', _id : 0 } }",
                @"{ $project : {
                        _v : { $map : {
                            input : { $cond : {
                                    if : { $eq : [{ $size : '$usersListTemp' }, 0] },
                                    then : [null],
                                    else : '$usersListTemp' } },
                            as : 'organizationUser',
                            in : {
                                TeamId : '$<>h__TransparentIdentifier3.<>h__TransparentIdentifier2.<>h__TransparentIdentifier1.<>h__TransparentIdentifier0.team.TeamId',
                                TeamName : '$<>h__TransparentIdentifier3.<>h__TransparentIdentifier2.<>h__TransparentIdentifier1.<>h__TransparentIdentifier0.team.TeamName',
                                OrganizationLogo : { $cond : {
                                    if : { $ne : ['$$organizationUser', null] },
                                    then : '$$organizationUser.ProfileImage',
                                    else : '$<>h__TransparentIdentifier3.<>h__TransparentIdentifier2.<>h__TransparentIdentifier1.<>h__TransparentIdentifier0.team.OrganizationLogo' } },
                                AllianceTeamId : '$<>h__TransparentIdentifier3.<>h__TransparentIdentifier2.<>h__TransparentIdentifier1.allianceMapping.AllianceTeamId' } } },
                        _id : 0 } }",
                "{ $unwind : '$_v' }");

            var results = queryable.ToList();
            var result = results.Single();
            result.TeamId.Should().Be(1);
            result.TeamName.Should().Be("team name");
            result.OrganizationLogo.Should().Be("profile image");
            result.AllianceTeamId.Should().Be(2);
        }

        private IMongoCollection<Team> CreateTeamsCollection()
        {
            var collection = GetCollection<Team>("teams");
            CreateCollection(
                collection,
                new Team { Id = 1, TeamId = 1, TeamName = "team name", OrganizationId = 3, OrganizationLogo = "organization logo" });
            return collection;
        }

        private IMongoCollection<TeamAllianceMapping> CreateTeamAllianceMappingsCollection()
        {
            var collection = GetCollection<TeamAllianceMapping>("teamAllianceMappings");
            CreateCollection(
                collection,
                new TeamAllianceMapping { Id = 1, TeamId = 1, AllianceTeamId = 2 });
            return collection;
        }

        private IMongoCollection<OrganizationAdmin> CreateOrganizationAdminsCollection()
        {
            var collection = GetCollection<OrganizationAdmin>("organizationAdmins");
            CreateCollection(
                collection,
                new OrganizationAdmin { Id = 3, OrganizationId = 3, UserId = 4 });
            return collection;
        }

        private IMongoCollection<User> CreateUsersCollection()
        {
            var collection = GetCollection<User>("users");
            CreateCollection(
                collection,
                new User { Id = 4, UserId = 4, ProfileImage = "profile image" });
            return collection;
        }

        private class Team
        {
            public int Id { get; set; }
            public int TeamId { get; set; }
            public string TeamName { get; set; }
            public int OrganizationId { get; set; }
            public string OrganizationLogo { get; set; }
        }

        private class TeamAllianceMapping
        {
            public int Id { get; set; }
            public int TeamId { get; set; }
            public int AllianceTeamId { get; set; }
        }

        private class OrganizationAdmin
        {
            public int Id { get; set; }
            public int OrganizationId { get; set; }
            public int UserId { get; set; }
        }

        private class User
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string ProfileImage { get; set; }
        }
    }
}
