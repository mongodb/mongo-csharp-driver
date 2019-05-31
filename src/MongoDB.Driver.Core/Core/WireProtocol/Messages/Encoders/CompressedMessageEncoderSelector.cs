/* Copyright 2019-present MongoDB Inc.
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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders
{
    /// <summary>
    /// Represents a message encoder selector for compressed messages.
    /// </summary>
    public class CompressedMessageEncoderSelector : IMessageEncoderSelector
    {
        private readonly IMessageEncoderSelector _originalEncoderSelector;

        /// <summary>
        /// Represents a compressed message encoder selector.
        /// </summary>
        /// <param name="originalEncoderSelector">The original encoder.</param>
        public CompressedMessageEncoderSelector(IMessageEncoderSelector originalEncoderSelector)
        {
            _originalEncoderSelector = Ensure.IsNotNull(originalEncoderSelector, nameof(originalEncoderSelector));
        }

        // public methods
        /// <inheritdoc />
        public IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetCompressedMessageEncoder(_originalEncoderSelector);
        }
    }
}
