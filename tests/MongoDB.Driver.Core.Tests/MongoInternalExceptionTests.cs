/* Copyright 2013-2016 MongoDB Inc.
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
using System.IO;
#if NET45
using System.Runtime.Serialization.Formatters.Binary;
#endif
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver
{
    public class MongoInternalExceptionTests
    {
        private Exception _innerException = new Exception("inner");
        private string _message = "message";

        [Fact]
        public void constructor_should_initialize_subject()
        {
            var subject = new MongoInternalException(_message);

            subject.InnerException.Should().BeNull();
            subject.Message.Should().BeSameAs(_message);
        }

        [Fact]
        public void constructor_with_innerException_should_initialize_subject()
        {
            var subject = new MongoInternalException(_message, _innerException);

            subject.InnerException.Should().BeSameAs(_innerException);
            subject.Message.Should().BeSameAs(_message);
        }

#if NET45
        [Fact]
        public void Serialization_should_work()
        {
            var subject = new MongoInternalException(_message, _innerException);

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, subject);
                stream.Position = 0;
                var rehydrated = (MongoInternalException)formatter.Deserialize(stream);

                rehydrated.InnerException.Message.Should().Be(subject.InnerException.Message); // Exception does not override Equals
                rehydrated.Message.Should().Be(subject.Message);
            }
        }
#endif
    }
}
