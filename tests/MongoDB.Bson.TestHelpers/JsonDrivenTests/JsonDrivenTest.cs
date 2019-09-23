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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Bson.TestHelpers.JsonDrivenTests
{
    public abstract class JsonDrivenTest
    {
        // protected fields
        protected Exception _actualException;
        protected BsonDocument _expectedException;
        protected BsonValue _expectedResult;
        protected Dictionary<string, object> _objectMap;

        // constructors
        protected JsonDrivenTest(Dictionary<string, object> objectMap = null)
        {
            _objectMap = objectMap;
        }

        // public methods
        public virtual void Act(CancellationToken cancellationToken)
        {
            try
            {
                CallMethod(cancellationToken);
            }
            catch (Exception exception)
            {
                _actualException = exception;
            }
        }

        public virtual async Task ActAsync(CancellationToken cancellationToken)
        {
            try
            {
                await CallMethodAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _actualException = exception;
            }
        }

        public virtual void Arrange(BsonDocument document)
        {
            if (document.Contains("arguments"))
            {
                SetArguments(document["arguments"].AsBsonDocument);
            }

            if (document.Contains("error"))
            {
                _expectedException = new BsonDocument(); // any exception will do
            }

            if (document.TryGetValue("result", out var result) || document.TryGetValue("results", out result))
            {
                ParseExpectedResult(result);
            }
        }

        public virtual void Assert()
        {
            if (_expectedException == null)
            {
                if (_actualException != null)
                {
                    throw new Exception("Unexpected exception was thrown.", _actualException);
                }

                if (_expectedResult != null)
                {
                    AssertResult();
                }
            }
            else
            {
                if (_actualException == null)
                {
                    throw new Exception("Expected an exception but none was thrown.");
                }
                AssertException();
            }
        }

        public void ThrowActualExceptionIfNotNull()
        {
            if (_actualException != null)
            {
                throw _actualException;
            }
        }

        // protected methods
        protected virtual void AssertException()
        {
            throw new NotImplementedException();
        }

        protected virtual void AssertResult()
        {
            throw new NotImplementedException();
        }

        protected virtual void CallMethod(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual Task CallMethodAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual void ParseExpectedResult(BsonValue value)
        {
            _expectedResult = value;
        }

        protected virtual void SetArgument(string name, BsonValue value)
        {
            throw new FormatException($"{GetType().Name} unexpected argument: \"{name}\".");
        }

        protected virtual void SetArguments(BsonDocument arguments)
        {
            foreach (var argument in arguments)
            {
                SetArgument(argument.Name, argument.Value);
            }
        }
    }
}
