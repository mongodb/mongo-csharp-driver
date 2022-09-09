using System.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4316Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Nullable_type_translate_should_work()
        {
            var collection = GetCollection<ModelContainNullableType>();
            var fluent = collection.Aggregate()
                .Group(x => new { Some = x.Some.Value, x.AnotherKeyGroup, x.Some.HasValue }, x => x.Select(t => t));

            var stages = Translate(collection, fluent);
            var expected = new []
            {
                "{ $group : {'_id' : { Some : '$Some', AnotherKeyGroup : '$AnotherKeyGroup', HasValue : { $ne : ['$Some', null] }}, __agg0 : { '$push' : '$$ROOT'}}}",
                "{ $project : { '_v' : '$__agg0', _id : 0 } }"
            };

            AssertStages(stages, expected);
        }

        [Fact]
        public void Type_contains_property_value_translate_should_work()
        {
            var collection = GetCollection<ModelWithoutNullable>();
            var fluent = collection.Aggregate()
                .Group(x => new { Key1 = x.Property.Value, Key2 = x.SomeData, Key3 = x.Property.HasValue}, x => x.Select(t => t));

            var stages = Translate(collection, fluent);
            var expected = new []
            {
                "{ $group : {'_id' : { Key1 : '$Property.Value', Key2 : '$SomeData', Key3 : '$Property.HasValue' }, __agg0 : { '$push' : '$$ROOT'}}}",
                "{ $project : { '_v' : '$__agg0', _id : 0 } }"
            };
            AssertStages(stages, expected);
        }

        public class ModelContainNullableType
        {
            public int? Some { get; set; }
            public float AnotherKeyGroup { get; set; }

        }

        public class ModelWithoutNullable
        {
            public CustomTypeLikeNullable Property { get; set; }
            public int SomeData { get; set; }
        }

        public class CustomTypeLikeNullable
        {
            public string Value { get; set; }
            public bool HasValue { get; set; }
        }
    }
}
