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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp3435Tests : LinqIntegrationTest<CSharp3435Tests.ClassFixture>
{
    public CSharp3435Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Where_should_work()
    {
        var queryable = CreateQueryable()
            .Where(x => x.NormalizedUsername == "PAPLABROS");

        var stages = Translate(Fixture.UserClaimCollection, queryable);
        AssertStages(
            stages,
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'Users', localField : '_outer.UserId', foreignField : '_id', as : '_inner' } }",
            "{ $project : { claim : '$_outer', users : '$_inner', _id : 0 } }",
            "{ $match : { 'claim.ClaimType' : 'Moderator' } }",
            "{ $project : { _v : { $arrayElemAt : ['$users', 0] }, _id : 0 } }",
            "{ $match : { '_v.NormalizedUsername' : 'PAPLABROS' } }");
    }

    [Fact]
    public void Where_with_Inject_should_work()
    {
        var filter = Builders<User>.Filter.Eq(x => x.NormalizedUsername, "PAPLABROS");
        var queryable = CreateQueryable()
            .Where(x => filter.Inject());

        var stages = Translate(Fixture.UserClaimCollection, queryable);
        AssertStages(
            stages,
            "{ $project : { _outer : '$$ROOT', _id : 0 } }",
            "{ $lookup : { from : 'Users', localField : '_outer.UserId', foreignField : '_id', as : '_inner' } }",
            "{ $project : { claim : '$_outer', users : '$_inner', _id : 0 } }",
            "{ $match : { 'claim.ClaimType' : 'Moderator' } }",
            "{ $project : { _v : { $arrayElemAt : ['$users', 0] }, _id : 0 } }",
            "{ $match : { '_v.NormalizedUsername' : 'PAPLABROS' } }");
    }

    public IQueryable<User> CreateQueryable()
    {
        var usersCollection = Fixture.UserCollection;
        var userClaimsCollection = Fixture.UserClaimCollection;

        var queryable =
            from claim in userClaimsCollection.AsQueryable()
            join user in usersCollection.AsQueryable() on claim.UserId equals user.Id into users
            where claim.ClaimType == "Moderator"
            select users.First();

        // this is the equivalent method syntax
        // var queryable = userClaimsCollection.AsQueryable()
        //     .GroupJoin(
        //         usersCollection.AsQueryable(),
        //         claim => claim.UserId,
        //         user => user.Id,
        //         (claim, users) => new { claim, users })
        //     .Where(x => x.claim.ClaimType == "Moderator")
        //     .Select(x => x.users.First());

        return queryable;
    }

    public class User
    {
        public int Id { get; set; }
        public string NormalizedUsername { get; set; }
    }

    public class UserClaim
    {
        public int Id { get; set; }
        public int UserId  { get; set; }
        public string ClaimType { get; set; }
    }

    public sealed class ClassFixture : MongoDatabaseFixture
    {
        public IMongoCollection<User> UserCollection { get; private set; }
        public IMongoCollection<UserClaim> UserClaimCollection { get; private set; }

        protected override void InitializeFixture()
        {
            UserCollection = CreateCollection<User>("Users");
            UserClaimCollection = CreateCollection<UserClaim>("UserClaims");
        }
    }
}
