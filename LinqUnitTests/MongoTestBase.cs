using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.IntegrationTests.Linq
{
    public abstract class MongoTestBase
    {
        public MongoServer Mongo { get; set; }

        public MongoDatabase DB
        {
            get
            {
                return this.Mongo["tests"];
            }
        }

        /// <summary>
        /// Comma separated list of collections to clean at startup.
        /// </summary>
        public abstract string TestCollections { get; }

        /// <summary>
        /// Override to add custom initialization code.
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// Override to add custom code to invoke during the test end.
        /// </summary>
        public virtual void OnDispose() { }

        /// <summary>
        /// Sets up the test environment.  You can either override this OnInit to add custom initialization.
        /// </summary>
        [TestFixtureSetUp]
        public virtual void Init()
        {
            this.Mongo = MongoServer.Create();
            this.Mongo.Connect();
            CleanDB();
            OnInit();
        }


        [TestFixtureTearDown]
        public virtual void Dispose()
        {
            OnDispose();
            this.Mongo.Disconnect();
        }

        protected void CleanDB()
        {
            foreach (string col in this.TestCollections.Split(','))
            {
                try
                {
                    DB.DropCollection(col.Trim());
                } catch
                {
                }
            }
        }

    }
}