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
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Core.Events
{
    public class ReflectionEventSubscriberTests
    {
        [Fact]
        public void Should_match_all_methods_matching_the_required_signature()
        {
            var subject = new ReflectionEventSubscriber(new EventTest());

            Action<int> i;
            subject.TryGetEventHandler(out i).Should().BeTrue();

            Action<string> str;
            subject.TryGetEventHandler(out str).Should().BeTrue();

            Action<byte> b;
            subject.TryGetEventHandler(out b).Should().BeFalse();

            Action<bool> boolean;
            subject.TryGetEventHandler(out boolean).Should().BeFalse();

            Action<bool> c;
            subject.TryGetEventHandler(out c).Should().BeFalse();
        }

        private class EventTest
        {
            public void Handle(int i)
            {

            }

            public void Handle(string s)
            {

            }

            public void WrongName(byte b)
            {

            }

            public bool Handle(bool k) // wrong signature
            {
                return k;
            }

            private void Handle(char c) // not public
            {

            }
        }
    }
}
