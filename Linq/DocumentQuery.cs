using System;
using MongoDB.Bson;

namespace MongoDB.Linq {

    /// <summary>
    /// This class is a construct for writing strongly typed query expressions for Document fields.
    /// It is not meant to be used outside of expressions, since most functions and operators return
    /// fake data and are only used to extract parameter information from expressions.
    /// </summary>
    internal class DocumentQuery {
        /// <summary>
        /// 
        /// </summary>
        private readonly string key;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentQuery"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="key">The key.</param>
        public DocumentQuery(BsonDocument document, string key) {
            this.key = key;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get { return key; } }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DocumentQuery a, string b) { return false; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DocumentQuery a, string b) { return false; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(string a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(string a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(DocumentQuery a, int b) { return false; }

        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >=(DocumentQuery a, int b) { return false; }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(DocumentQuery a, int b) { return false; }

        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <=(DocumentQuery a, int b) { return false; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DocumentQuery a, int b) { return false; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DocumentQuery a, int b) { return false; }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(int a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >=(int a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(int a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <=(int a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(int a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(int a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(DocumentQuery a, double b) { return false; }

        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >=(DocumentQuery a, double b) { return false; }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(DocumentQuery a, double b) { return false; }

        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <=(DocumentQuery a, double b) { return false; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DocumentQuery a, double b) { return false; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DocumentQuery a, double b) { return false; }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(double a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >=(double a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(double a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <=(double a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(double a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(double a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(DocumentQuery a, DateTime b) { return false; }
        
        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >=(DocumentQuery a, DateTime b) { return false; }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(DocumentQuery a, DateTime b) { return false; }

        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <=(DocumentQuery a, DateTime b) { return false; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DocumentQuery a, DateTime b) { return false; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DocumentQuery a, DateTime b) { return false; }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >(DateTime a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator >=(DateTime a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <(DateTime a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator <=(DateTime a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DateTime a, DocumentQuery b) { return false; }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DateTime a, DocumentQuery b) { return false; }

        /// <summary>
        /// Equalses the specified other.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool Equals(DocumentQuery other)
        {
            if(ReferenceEquals(null, other))
                return false;
            if(ReferenceEquals(this, other))
                return true;
            return Equals(other.key, key);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <paramref name="obj"/> parameter is null.
        /// </exception>
        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj))
                return false;
            if(ReferenceEquals(this, obj))
                return true;
            if(obj.GetType() != typeof(DocumentQuery))
                return false;
            return Equals((DocumentQuery)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (key != null ? key.GetHashCode() : 0);
        }
    }
}
