/* Copyright 2018-present MongoDB Inc.
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

using System.Collections.Generic;
using FluentAssertions;

namespace MongoDB.Bson.TestHelpers.JsonDrivenTests
{
    public abstract class AspectAsserter
    {
        // public methods
        public abstract void AssertAspects(object actualValue, BsonDocument aspects);

        public virtual void ConfigurePlaceholders(KeyValuePair<string, BsonValue>[] placeholders)
        {
            // do nothing by default
        }
    }

    public abstract class AspectAsserter<TActual> : AspectAsserter
    {
        // public methods
        public override void AssertAspects(object actualValue, BsonDocument aspects)
        {
            actualValue.Should().BeOfType<TActual>();
            AssertAspects((TActual)actualValue, aspects);
        }

        public void AssertAspects(TActual actualValue, BsonDocument aspects)
        {
            foreach (var aspect in aspects)
            {
                AssertAspect(actualValue, aspect.Name, aspect.Value);
            }
        }

        // protected methods
        protected abstract void AssertAspect(TActual actualValue, string name, BsonValue expectedValue);
    }
}
