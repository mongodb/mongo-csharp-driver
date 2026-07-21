using Moq;

namespace MongoDB.Driver.Tests.GridFS
{
    internal static class MockHelpers
    {
        public static IMongoDatabase GetMongoDatabaseMock()
        {
            var client = new Mock<IMongoClient>() { DefaultValue = DefaultValue.Empty };
            client.SetupGet(c => c.Settings).Returns(new MongoClientSettings());
            var database = new Mock<IMongoDatabase> { DefaultValue = DefaultValue.Mock };
            database.SetupGet(d => d.Client).Returns(client.Object);

            return database.Object;
        }
    }
}
