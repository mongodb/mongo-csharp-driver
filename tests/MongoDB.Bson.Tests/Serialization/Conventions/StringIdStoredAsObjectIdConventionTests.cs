using Xunit;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class StringIdStoredAsObjectIdConventionTests
    {
        BsonMemberMap SampleMap<T>() => new BsonClassMap<T>(cm => cm.AutoMap()).GetMemberMap("Id");

        [Fact]
        public void Apply_StringId_SetsSerializer()
        {
            var target = new StringIdStoredAsObjectIdConvention();
            var subject = SampleMap<TestClassWithStringId>();

            target.Apply(subject);

            Assert.IsType<StringSerializer>(subject.GetSerializer());
        }

        [Fact]
        public void Apply_StringId_SetsIdGenerator()
        {
            var target = new StringIdStoredAsObjectIdConvention();
            var subject = SampleMap<TestClassWithStringId>();

            target.Apply(subject);

            Assert.IsType<StringObjectIdGenerator>(subject.IdGenerator);
        }

        [Fact]
        public void Apply_ExistingIdGenerator_DoesNotApply()
        {
            var target = new StringIdStoredAsObjectIdConvention();
            var subject = SampleMap<TestClassWithStringId>();
            subject.SetIdGenerator(CombGuidGenerator.Instance);

            target.Apply(subject);

            Assert.IsType<CombGuidGenerator>(subject.IdGenerator);
        }

        [Fact]
        public void Apply_NotStringSerializer_DoesNotApply()
        {
            var target = new StringIdStoredAsObjectIdConvention();
            var subject = SampleMap<TestClassWithStringId>();
            subject.SetSerializer(new FakeStringSerializer());

            target.Apply(subject);

            Assert.IsType<FakeStringSerializer>(subject.GetSerializer());
        }


        [Fact]
        public void Apply_IntId_LeavesSerializer()
        {
            var target = new StringIdStoredAsObjectIdConvention();
            var subject = SampleMap<TestClassWithIntId>();

            target.Apply(subject);

            Assert.IsNotType<StringSerializer>(subject.GetSerializer());
        }

        [Fact]
        public void Apply_IntId_NoIdGenerator()
        {
            var target = new StringIdStoredAsObjectIdConvention();
            var subject = SampleMap<TestClassWithIntId>();

            target.Apply(subject);

            Assert.Null(subject.IdGenerator);
        }


        public class TestClassWithStringId { public string Id; }

        public class TestClassWithIntId { public int Id; }

        class FakeStringSerializer : SealedClassSerializerBase<string>
        {
            public BsonType Representation => BsonType.String;
        }
    }
}



