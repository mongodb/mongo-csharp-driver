using System;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Exceptions;
using MongoDB.Driver.Core.Tests.Helpers;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Authentication
{
    [TestFixture]
    public class MongoDBX509AuthenticatorTests
    {
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void Constructor_should_throw_an_ArgumentException_when_username_is_null_or_empty(string username)
        {
            Action act = () => new MongoDBX509Authenticator(username);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void AuthenticateAsync_should_throw_an_AuthenticationException_when_authentication_fails()
        {
            var reply = ReplyMessageHelper.BuildNoDocumentsReturned<RawBsonDocument>();
            var connection = new MockRootConnection();
            connection.AddReplyMessage(reply);

            var subject = new MongoDBX509Authenticator("username");

            Action act = () => subject.AuthenticateAsync(connection, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<AuthenticationException>();
        }

        [Test]
        public void AuthenticateAsync_should_not_throw_when_authentication_succeeds()
        {
            var reply = ReplyMessageHelper.BuildSuccess<RawBsonDocument>(
                RawBsonDocumentHelper.FromBsonDocument(new BsonDocument("ok", 1)));

            var connection = new MockRootConnection();
            connection.AddReplyMessage(reply);

            var subject = new MongoDBX509Authenticator("username");

            Action act = () => subject.AuthenticateAsync(connection, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldNotThrow();
        }
    }
}