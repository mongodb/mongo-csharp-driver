/*
 * Copyright 2019–present MongoDB, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;

namespace MongoDB.Driver.Encryption
{
    /// <summary>
    /// A collection of kms requests to make.
    /// When all requests are done, <c>MarkDone</c> must be called.
    /// </summary>
    internal class KmsRequestCollection : IReadOnlyCollection<KmsRequest>
    {
        private readonly List<KmsRequest> _requests;
        private readonly CryptContext _parent;

        internal KmsRequestCollection(List<KmsRequest> requests, CryptContext parent)
        {
            _requests = requests;
            _parent = parent;
        }

        int IReadOnlyCollection<KmsRequest>.Count => _requests.Count;

        IEnumerator<KmsRequest> IEnumerable<KmsRequest>.GetEnumerator()
        {
            return _requests.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _requests.GetEnumerator();
        }

        /// <summary>
        /// Marks alls the KMS requests as complete.
        /// </summary>
        public void MarkDone()
        {
            _parent.MarkKmsDone();
        }
    }
}
