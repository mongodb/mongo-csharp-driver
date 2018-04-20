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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public abstract class JsonDrivenClientTest
    {
        // protected fields
        protected readonly IMongoClient _client;
        protected BsonValue _expectedResult;

        // private fields
        private Exception _exception;
        private string _expectedErrorCodeName;
        private string _expectedErrorContains;
        private IClientSessionHandle _session;
        private readonly Dictionary<string, IClientSessionHandle> _sessionMap;

        // protected constructors
        protected JsonDrivenClientTest(IMongoClient client, Dictionary<string, IClientSessionHandle> sessionMap)
        {
            _client = client;
            _sessionMap = sessionMap;
        }

        // public properties
        public IClientSessionHandle Session
        {
            get { return _session; }
            set { _session = value; }
        }

        // public methods
        public virtual void Act(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                RecordException(() => CallMethod(cancellationToken));
            }
            else
            {
                RecordException(() => CallMethod(_session, cancellationToken));
            }
        }

        public virtual async Task ActAsync(CancellationToken cancellationToken)
        {
            if (_session == null)
            {
                await RecordExceptionAsync(() => CallMethodAsync(cancellationToken)).ConfigureAwait(false);
            }
            else
            {
                await RecordExceptionAsync(() => CallMethodAsync(_session, cancellationToken)).ConfigureAwait(false);
            }
        }

        public virtual void Arrange(BsonDocument document)
        {
            if (document.Contains("arguments"))
            {
                SetArguments(document["arguments"].AsBsonDocument);
            }

            if (document.Contains("result"))
            {
                SetResult(document["result"]);
            }
        }

        public virtual void Assert()
        {
            if (_exception != null)
            {
                AssertException();
            }
            else if (_expectedResult != null)
            {
                AssertResult();
            }
        }

        // protected methods
        protected virtual void AssertException()
        {
            if (_expectedErrorCodeName != null)
            {
                var commandException = _exception as MongoCommandException;
                if (commandException != null && commandException.CodeName == _expectedErrorCodeName)
                {
                    return;
                }
            }

            if (_expectedErrorContains != null)
            {
                if (_exception.Message.IndexOf(_expectedErrorContains, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return;
                }
            }

            throw new Exception("Unexpected exception was thrown.", _exception);
        }

        protected virtual void AssertResult()
        {
            throw new FormatException($"{GetType().Name} doesn't know how to assert expected result: {_expectedResult.ToJson()}.");
        }

        protected virtual void CallMethod(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected virtual void CallMethod(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected virtual Task CallMethodAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected virtual Task CallMethodAsync(IClientSessionHandle session, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        protected virtual void SetArgument(string name, BsonValue value)
        {
            switch (name)
            {
                case "session":
                    _session = _sessionMap[value.AsString];
                    break;

                default:
                    throw new FormatException($"{GetType().Name} unexpected argument: \"{name}\".");
            }
        }

        protected virtual void SetArguments(BsonDocument arguments)
        {
            foreach (var argument in arguments)
            {
                SetArgument(argument.Name, argument.Value);
            }
        }

        protected virtual void SetResult(BsonValue value)
        {
            var document = value as BsonDocument;
            if (document != null)
            {
                if (document.Names.SequenceEqual(new[] { "errorCodeName" }))
                {
                    _expectedErrorCodeName = document["errorCodeName"].AsString;
                    return;
                }

                if (document.Names.SequenceEqual(new[] { "errorContains" }))
                {
                    _expectedErrorContains = document["errorContains"].AsString;
                    return;
                }
            }

            _expectedResult = value;
        }

        // private methods
        private void RecordException(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                _exception = exception;
            }
        }

        private async Task RecordExceptionAsync(Func<Task> actionAsync)
        {
            try
            {
                await actionAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _exception = exception;
            }
        }
    }
}
