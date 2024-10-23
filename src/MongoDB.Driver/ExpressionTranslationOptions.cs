/* Copyright 2016-present MongoDB Inc.
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
*
*/

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for controlling translation from .NET expression trees into MongoDB expressions.
    /// </summary>
    public sealed class ExpressionTranslationOptions
    {
        /// <summary>
        /// Gets or sets the server version to target when translating Expressions.
        /// </summary>
        public ServerVersion? CompatibilityLevel { get; set; }

        /// <summary>
        /// Gets or sets whether client side projections are enabled.
        /// </summary>
        public bool? EnableClientSideProjections { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                GetType().Equals(obj.GetType()) &&
                obj is ExpressionTranslationOptions other &&
                CompatibilityLevel.Equals(other.CompatibilityLevel) &&
                EnableClientSideProjections.Equals(other.EnableClientSideProjections);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public override string ToString()
        {
            var compatibilityLevel = CompatibilityLevel.HasValue ? CompatibilityLevel.ToString() : "null";
            var enableClientSideProjections = EnableClientSideProjections.HasValue ? EnableClientSideProjections.ToString() : "null";
            return $"{{ CompatibilityLevel = {compatibilityLevel}, EnableClientSideProjections = {enableClientSideProjections} }}";
        }
    }

    internal static class ExpressionTranslationOptionsExtensions
    {
        public static ExpressionTranslationOptions AddMissingOptionsFrom(this ExpressionTranslationOptions translationOptions, ExpressionTranslationOptions from)
        {
            if (translationOptions == null || from == null)
            {
                return translationOptions ?? from;
            }
            else
            {
                return new ExpressionTranslationOptions
                {
                    CompatibilityLevel = translationOptions.CompatibilityLevel ?? from.CompatibilityLevel,
                    EnableClientSideProjections = translationOptions.EnableClientSideProjections ?? from.EnableClientSideProjections
                };
            }
        }
    }
}
