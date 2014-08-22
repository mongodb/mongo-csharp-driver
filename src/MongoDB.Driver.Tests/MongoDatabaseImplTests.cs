using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Tests;
using NUnit.Framework;
using FluentAssertions;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    public class MongoDatabaseImplTests
    {
        private IMongoDatabase _subject;

        [SetUp]
        public void Setup()
        {
            _subject = Configuration.TestClient.GetDatabase("foo");
        }

        [Test]
        public void DatabaseName_should_be_set()
        {
            _subject.DatabaseName.Should().Be("foo");
        }

        [Test]
        public void Settings_should_be_set()
        {
            _subject.Settings.Should().NotBeNull();
        }

        [Test]
        public async void RunCommand_should_run_a_read_command()
        {
            var result = await _subject.RunCommandAsync<BsonDocument>(new BsonDocument("count", "bar"));

            result.Should().NotBeNull();
            result["ok"].Should().Be(1);
        }

        [Test]
        public async void RunCommand_should_run_a_non_read_command()
        {
            var result = await _subject.RunCommandAsync<BsonDocument>(new BsonDocument("isMaster", 1));

            result.Should().NotBeNull();
            result["ok"].Should().Be(1);
        }
    }
}