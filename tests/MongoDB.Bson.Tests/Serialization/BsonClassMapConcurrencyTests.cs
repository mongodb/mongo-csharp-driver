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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonClassMapConcurrencyTests
    {
        [Fact]
        public void LookupClassMap_should_not_deadlock_when_serializer_resolution_triggers_nested_class_map_lookup()
        {
            var mre1 = new ManualResetEventSlim(false);
            var mre2 = new ManualResetEventSlim(false);

            DeadlockTriggeringOptionsAttribute.__mre1 = mre1;
            DeadlockTriggeringOptionsAttribute.__mre2 = mre2;

            var taskB = Task.Run(() => BsonClassMap.LookupClassMap(typeof(ClassB)));

            mre1.Wait(); // Wait until taskB acquires the lock on Lazy<IBsonSerializer<ClassA>>._state

            var taskA = Task.Run(() => BsonClassMap.LookupClassMap(typeof(ClassC)));

            Thread.Sleep(2000); // Wait until taskA acquires write-lock on BsonSerializer.ConfigLock

            mre2.Set(); // Release taskB

            var completed = Task.WhenAll(taskA, taskB).Wait(TimeSpan.FromSeconds(10));

            completed.Should().BeTrue("LookupClassMap has deadlocked");
        }

        // nested types
        private class ClassA
        {
            [DeadlockTriggeringOptions]
            public int X { get; set; }
        }

        private class ClassB
        {
            [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
            public Dictionary<string, ClassA> Dictionary { get; set; } = new();
        }

        private class ClassC : ClassB
        {
        }

        private class DeadlockTriggeringOptionsAttribute : BsonSerializationOptionsAttribute
        {
            internal static ManualResetEventSlim __mre1;
            internal static ManualResetEventSlim __mre2;

            protected override IBsonSerializer Apply(IBsonSerializer serializer)
            {
                var mre1 = Interlocked.Exchange(ref __mre1, null);
                var mre2 = Interlocked.Exchange(ref __mre2, null);

                mre1?.Set(); // Signal that taskB has acquired the lock on Lazy<IBsonSerializer<ClassA>>._state

                mre2?.Wait(); // Wait until taskA acquires write-lock on BsonSerializer.ConfigLock

                return serializer;
            }
        }
    }
}
