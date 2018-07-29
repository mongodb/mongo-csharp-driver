using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class IncludeReadOnlyMemberConventionTests
    { 
        private IncludeReadOnlyMemberConvention _subject;

        public IncludeReadOnlyMemberConventionTests()
        {
            _subject = new IncludeReadOnlyMemberConvention();
        }

        [Fact]
        public void TestMapsAllTheReadAndWriteFieldsAndProperties()
        {
            var classMap = new BsonClassMap<TestClass>();

            _subject.Apply(classMap);

            Assert.Equal(4, classMap.DeclaredMemberMaps.Count());

            Assert.NotNull(classMap.GetMemberMap(x => x.Mapped1));
            Assert.NotNull(classMap.GetMemberMap(x => x.Mapped2));
            Assert.NotNull(classMap.GetMemberMap(x => x.Mapped3));
            Assert.NotNull(classMap.GetMemberMap(x => x.Mapped4));

            Assert.Null(classMap.GetMemberMap(x => x.NotMapped1));
        }

        private class TestClass
        {
            public string Mapped1 { get; set; }

            public string Mapped2 = "blah";

            // yes, we'll map this because we know how to set it and part of it is public...
            public string Mapped3 { get; private set; }

            public string Mapped4
            {
                get { return "blah"; }
            }

            public readonly string NotMapped1 = "blah";
        }
    }
}