using FluentAssertions;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp4675Tests
    {
        public class A
        {
            public int X { get; set; }
        }

        [Fact]
        public void SafeContent_element_should_be_ignored()
        {
            var subject = BsonSerializer.Deserialize<A>("{ X : 1, \"__safeContent__\": [] }");

            subject.X.Should().Be(1);
        }
    }
}
