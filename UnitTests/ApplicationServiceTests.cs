using System.Collections.Generic;
using System.Linq;
using Client;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Moq;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public abstract class ApplicationServiceTests
    {
        protected readonly List<Person> PeopleOver21YearsOld;

        protected ApplicationServiceTests()
        {
            PeopleOver21YearsOld = new List<Person>
            {
                new Person {Id = "1", Age = 25, Name = "Walter"},
                new Person {Id = "2", Age = 36, Name = "Euan"}
            };
        }

        [Test]
        public void GetPeopleOver21YearsOld()
        {
            var applcationService = new ApplicationService(GetDatabase());

            Assert.AreEqual(2, applcationService.GetPeopleOver(21).Count());

            Verify();
        }

        public abstract IMongoDatabase GetDatabase();
        public abstract void Verify();
    }

    public class ApplicationServiceTests_UNIT : ApplicationServiceTests
    {
        private readonly Mock<IMongoDatabase> _databaseMock;
        private readonly Mock<IMongoCollection<Person>> _peopleCollectionMock;
        private readonly Mock<IMongoCursor<Person>> _cursorMock;
        private readonly IMongoQuery _expectedQuery;

        public ApplicationServiceTests_UNIT()
        {
            _expectedQuery = Query.GT("Age", 21);
            _databaseMock = new Mock<IMongoDatabase>();
            _peopleCollectionMock = new Mock<IMongoCollection<Person>>();
            _cursorMock = new Mock<IMongoCursor<Person>>();

            _cursorMock.Setup(m => m.GetEnumerator()).Returns(PeopleOver21YearsOld.GetEnumerator());
            
            _databaseMock.Setup(m => m.GetCollection<Person>(CollectionNames.People))
                .Returns(_peopleCollectionMock.Object);

            _peopleCollectionMock.Setup(
                m => m.FindAs<Person>(It.Is<IMongoQuery>(query => query.ToString().Equals(_expectedQuery.ToString()))))
                .Returns(_cursorMock.Object);
        }

        public override IMongoDatabase GetDatabase()
        {
            return _databaseMock.Object;
        }

        public override void Verify()
        {
            _databaseMock.VerifyAll();
            _peopleCollectionMock.VerifyAll();
            _cursorMock.VerifyAll();
        }
    }

    public class ApplicationServiceTests_INTEGRATION : ApplicationServiceTests
    {
        private readonly MongoDatabase _database;

        public ApplicationServiceTests_INTEGRATION()
        {
            _database = MongoDatabase.Create("mongodb://localhost:27017/TestInt");
            
            if (_database.CollectionExists(CollectionNames.People))
                _database.DropCollection(CollectionNames.People);

            var peopleCollection = _database.GetCollection<Person>(CollectionNames.People);

            foreach (var person in PeopleOver21YearsOld)
                peopleCollection.Insert(person);
        }

        public override IMongoDatabase GetDatabase()
        {
            return _database;
        }

        public override void Verify()
        {}
    }
}
