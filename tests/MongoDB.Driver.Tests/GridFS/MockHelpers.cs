using Moq;

namespace MongoDB.Driver.GridFS.Tests
{
    internal static class MockHelpers
    {
        public static IMongoDatabase GetMockMongoDatabaseMock()
        {
            var client = new Mock<IMongoClient>() { DefaultValue = DefaultValue.Empty };
            var database = new Mock<IMongoDatabase> { DefaultValue = DefaultValue.Mock };
            database.SetupGet(d => d.Client).Returns(client.Object);

            return database.Object;
        }
    }
}
