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

using System;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for replacing a single document.
    /// </summary>
    public sealed class ReplaceOptions
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
                    IsUpsert = updateOptions.IsUpsert
                };
            }
        }
        #endregion

        // fields
        private bool? _bypassDocumentValidation;
        private Collation _collation;
        private bool _isUpsert;

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
        /// Gets or sets a value indicating whether to insert the document if it doesn't already exist.
        /// </summary>
        public bool IsUpsert
        {
            get { return _isUpsert; }
            set { _isUpsert = value; }
        }
    }
}
