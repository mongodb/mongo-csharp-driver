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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the options parameter for <see cref="Mql.DeserializeEJson{TInput, TOutput}(TInput, DeserializeEJsonOptions{TOutput})"/>.
    /// </summary>
    public abstract class DeserializeEJsonOptions
    {
        internal abstract bool OnErrorWasSet(out object onError);
    }

    /// <summary>
    /// Represents the options parameter for <see cref="Mql.DeserializeEJson{TInput, TOutput}(TInput, DeserializeEJsonOptions{TOutput})"/>.
    /// This class allows to set 'onError'.
    /// </summary>
    /// <typeparam name="TOutput">The type of 'onError'.</typeparam>
    public class DeserializeEJsonOptions<TOutput> : DeserializeEJsonOptions
    {
        private TOutput _onError;
        private bool _onErrorWasSet;

        /// <summary>
        /// The onError parameter.
        /// </summary>
        public TOutput OnError
        {
            get => _onError;
            set
            {
                _onError = value;
                _onErrorWasSet = true;
            }
        }

        internal override bool OnErrorWasSet(out object onError)
        {
            onError = _onError;
            return _onErrorWasSet;
        }
    }
}
