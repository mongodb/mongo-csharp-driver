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

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the options parameter for <see cref="Mql.Convert{TFrom, TTo}(TFrom, ConvertOptions{TTo})"/>.
    /// </summary>
    public abstract class ConvertOptions
    {
        private ByteOrder? _byteOrder;
        private string _format;
        private BsonBinarySubType? _subType;

        /// <summary>
        /// The byteOrder parameter.
        /// </summary>
        public ByteOrder? ByteOrder
        {
            get => _byteOrder;
            set => _byteOrder = value;
        }

        /// <summary>
        /// The format parameter.
        /// </summary>
        public string Format
        {
            get => _format;
            set => _format = value;
        }

        /// <summary>
        /// The subType parameter.
        /// </summary>
        public BsonBinarySubType? SubType
        {
            get => _subType;
            set => _subType = value;
        }

        internal abstract bool OnErrorWasSet(out object onError);

        internal abstract bool OnNullWasSet(out object onNull);
    }

    /// <summary>
    /// Represents the options parameter for <see cref="Mql.Convert{TFrom, TTo}(TFrom, ConvertOptions{TTo})"/>.
    /// This class allows to set 'onError' and 'onNull'.
    /// </summary>
    /// <typeparam name="TTo"> The type of 'onError' and 'onNull'.</typeparam>
    public class ConvertOptions<TTo> : ConvertOptions
    {
        private TTo _onError;
        private bool _onErrorWasSet;
        private TTo _onNull;
        private bool _onNullWasSet;

        /// <summary>
        /// The onError parameter.
        /// </summary>
        public TTo OnError
        {
            get => _onError;
            set
            {
                _onError = value;
                _onErrorWasSet = true;
            }
        }

        /// <summary>
        /// The onNull parameter.
        /// </summary>
        public TTo OnNull
        {
            get => _onNull;
            set
            {
                _onNull = value;
                _onNullWasSet = true;
            }
        }

        internal override bool OnErrorWasSet(out object onError)
        {
            onError = _onError;
            return _onErrorWasSet;
        }

        internal override bool OnNullWasSet(out object onNull)
        {
            onNull = _onNull;
            return _onNullWasSet;
        }
    }

    /// <summary>
    /// Represents the byte order of binary data when converting to/from numerical types using <see cref="Mql.Convert{TFrom, TTo}(TFrom, ConvertOptions{TTo})"/>.
    /// </summary>
    public enum ByteOrder
    {
        /// <summary>
        /// Big endian order.
        /// </summary>
        BigEndian,
        /// <summary>
        /// Little endian order.
        /// </summary>
        LittleEndian,
    }
}