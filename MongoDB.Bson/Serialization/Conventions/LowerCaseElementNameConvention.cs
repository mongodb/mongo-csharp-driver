using System;
using System.Reflection;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Represents an element name convention where the element name is the member name being all lower cased.
    /// </summary>
    public class LowerCaseElementNameConvention : IElementNameConvention
    {
        /// <summary>
        /// Gets the element name for a member.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The element name.</returns>
        public string GetElementName(MemberInfo member)
        {
            return member.Name.ToLowerInvariant();
        }
    }
}