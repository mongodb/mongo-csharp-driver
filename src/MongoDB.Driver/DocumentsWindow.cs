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

using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a documents window for a SetWindowFields window method.
    /// </summary>
    public sealed class DocumentsWindow : SetWindowFieldsWindow
    {
        #region static
        private static readonly KeywordDocumentsWindowBoundary __current = new KeywordDocumentsWindowBoundary("current");
        private static readonly KeywordDocumentsWindowBoundary __unbounded = new KeywordDocumentsWindowBoundary("unbounded");

        /// <summary>
        /// Returns a "current" documents window boundary.
        /// </summary>
        public static KeywordDocumentsWindowBoundary Current => __current;

        /// <summary>
        /// Returns an "unbounded" documents window boundary.
        /// </summary>
        public static KeywordDocumentsWindowBoundary Unbounded => __unbounded;

        /// <summary>
        /// Creates a documents window.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A documents window.</returns>
        public static DocumentsWindow Create(int lowerBoundary, int upperBoundary)
        {
            return new DocumentsWindow(new PositionDocumentsWindowBoundary(lowerBoundary), new PositionDocumentsWindowBoundary(upperBoundary));
        }

        /// <summary>
        /// Creates a documents window.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A documents window.</returns>
        public static DocumentsWindow Create(int lowerBoundary, KeywordDocumentsWindowBoundary upperBoundary)
        {
            return new DocumentsWindow(new PositionDocumentsWindowBoundary(lowerBoundary), upperBoundary);
        }

        /// <summary>
        /// Creates a documents window.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A documents window.</returns>
        public static DocumentsWindow Create(KeywordDocumentsWindowBoundary lowerBoundary, int upperBoundary)
        {
            return new DocumentsWindow(lowerBoundary, new PositionDocumentsWindowBoundary(upperBoundary));
        }

        /// <summary>
        /// Creates a documents window.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        /// <returns>A documents window.</returns>
        public static DocumentsWindow Create(KeywordDocumentsWindowBoundary lowerBoundary, KeywordDocumentsWindowBoundary upperBoundary)
        {
            return new DocumentsWindow(lowerBoundary, upperBoundary);
        }
        #endregion

        private readonly DocumentsWindowBoundary _lowerBoundary;
        private readonly DocumentsWindowBoundary _upperBoundary;

        /// <summary>
        /// Initializes an instance of DocumentsWindow.
        /// </summary>
        /// <param name="lowerBoundary">The lower boundary.</param>
        /// <param name="upperBoundary">The upper boundary.</param>
        internal DocumentsWindow(DocumentsWindowBoundary lowerBoundary, DocumentsWindowBoundary upperBoundary)
        {
            _lowerBoundary = Ensure.IsNotNull(lowerBoundary, nameof(lowerBoundary));
            _upperBoundary = Ensure.IsNotNull(upperBoundary, nameof(upperBoundary));
        }

        /// <summary>
        /// The lower boundary.
        /// </summary>
        public DocumentsWindowBoundary LowerBoundary => _lowerBoundary;

        /// <summary>
        /// The upper boundary.
        /// </summary>
        public DocumentsWindowBoundary UpperBoundary => _upperBoundary;

        /// <inheritdoc/>
        public override string ToString() => $"documents : [{_lowerBoundary}, {_upperBoundary}]";
    }
}
