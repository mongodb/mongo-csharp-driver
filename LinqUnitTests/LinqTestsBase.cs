using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.LinqUnitTests
{
    public class LinqTestsBase : MongoTestBase
    {
        public override string TestCollections
        {
            get { return "people"; }
        }

        protected MongoCollection<Person> Collection;
        protected MongoCollection DocumentCollection;

        [SetUp]
        public virtual void TestSetup()
        {
            Collection = DB.GetCollection<Person>("people");
            DocumentCollection = DB.GetCollection("people");
        }
    }
}
