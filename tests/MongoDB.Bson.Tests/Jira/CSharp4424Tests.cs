/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using Shouldly;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.Jira
{
    public class CSharp4424Tests
    {
        [Fact]
        public void TryRegisterClassMap_with_no_arguments_can_be_called_more_than_once()
        {
            var result = BsonClassMap.TryRegisterClassMap<C>();
            result.ShouldBeTrue();

            result = BsonClassMap.TryRegisterClassMap<C>();
            result.ShouldBeFalse();
        }

        [Fact]
        public void TryRegisterClassMap_with_classMap_can_be_called_more_than_once()
        {
            var classMap = new BsonClassMap<D>(cm => cm.AutoMap());

            var result = BsonClassMap.TryRegisterClassMap(classMap);
            result.ShouldBeTrue();

            result = BsonClassMap.TryRegisterClassMap(classMap);
            result.ShouldBeFalse();
        }

        [Fact]
        public void TryRegisterClassMap_with_classMapInitializer_can_be_called_more_than_once()
        {
            var classMapInitializerCallCount = 0;

            var result = BsonClassMap.TryRegisterClassMap<E>(ClassMapInitializer);
            result.ShouldBeTrue();
            classMapInitializerCallCount.ShouldBe(1);

            result = BsonClassMap.TryRegisterClassMap<E>(ClassMapInitializer);
            result.ShouldBeFalse();
            classMapInitializerCallCount.ShouldBe(1);

            void ClassMapInitializer(BsonClassMap<E> cm)
            {
                classMapInitializerCallCount++;
                cm.AutoMap();
            }
        }

        [Fact]
        public void TryRegisterClassMap_with_classMapFactory_should_only_call_factory_once()
        {
            var classMapFactoryCallCount = 0;

            var result = BsonClassMap.TryRegisterClassMap(ClassMapFactory);
            result.ShouldBeTrue();
            classMapFactoryCallCount.ShouldBe(1);

            result = BsonClassMap.TryRegisterClassMap(ClassMapFactory);
            result.ShouldBeFalse();
            classMapFactoryCallCount.ShouldBe(1);

            BsonClassMap<F> ClassMapFactory()
            {
                classMapFactoryCallCount++;
                var classMap = new BsonClassMap<F>();
                classMap.AutoMap();
                return classMap;
            }
        }

        public class C
        {
        }

        public class D
        {
        }

        public class E
        {
        }

        public class F
        {
        }
    }
}
