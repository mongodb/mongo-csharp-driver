﻿/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for replacing a single document.
    /// </summary>
    public class ReplaceOptions
    {
        #region static
        // public static methods
        /// <summary>
        /// Creates a new ReplaceOptions from an UpdateOptions.
        /// </summary>
        /// <param name="updateOptions">The update options.</param>
        /// <returns>A ReplaceOptions.</returns>
        internal static ReplaceOptions From(UpdateOptions updateOptions)
        {
            if (updateOptions == null)
            {
                return null;
            }
            else
            {
                if (updateOptions.ArrayFilters != null)
                {
                    throw new ArgumentException("ArrayFilters cannot be used with ReplaceOne.", nameof(updateOptions));
                }

                return new ReplaceOptions
                {
                    BypassDocumentValidation = updateOptions.BypassDocumentValidation,
                    Collation = updateOptions.Collation,
                    Hint = updateOptions.Hint,
                    IsUpsert = updateOptions.IsUpsert,
                    Let = updateOptions.Let,
                    Timeout = updateOptions.Timeout
                };
            }
        }
        #endregion

        // fields
        private bool? _bypassDocumentValidation;
        private Collation _collation;
        private BsonValue _comment;
        private BsonValue _hint;
        private bool _isUpsert;
        private BsonDocument _let;
        private TimeSpan? _timeout;

        // properties
        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        public bool? BypassDocumentValidation
        {
            get { return _bypassDocumentValidation; }
            set { _bypassDocumentValidation = value; }
        }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        public BsonValue Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        /// <summary>
        /// Gets or sets the hint.
        /// </summary>
        public BsonValue Hint
        {
            get { return _hint; }
            set { _hint = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to insert the document if it doesn't already exist.
        /// </summary>
        public bool IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
        }

        /// <summary>
        /// Gets or sets the let document.
        /// </summary>
        public BsonDocument Let
        {
            get { return _let; }
            set { _let = value; }
        }

        /// <summary>
        /// Gets or sets the operation timeout.
        /// </summary>
        // TODO: CSOT: Make it public when CSOT will be ready for GA
        internal TimeSpan? Timeout
        {
            get => _timeout;
            set => _timeout = Ensure.IsNullOrValidTimeout(value, nameof(Timeout));
        }
    }

    /// <summary>
    /// Options for replacing a single document and specifying a sort order.
    /// </summary>
    public sealed class ReplaceOptions<T> : ReplaceOptions
    {
        private SortDefinition<T> _sort;

        /// <summary>
        /// Gets or sets the sort definition.
        /// </summary>
        public SortDefinition<T> Sort
        {
            get { return _sort; }
            set { _sort = value; }
        }
    }
}
