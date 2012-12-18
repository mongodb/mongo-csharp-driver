using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MongoDB.Driver.Security
{
    /// <summary>
    /// An exception thrown during login and privilege negotiation.
    /// </summary>
    [Serializable]
    public class MongoSecurityException : MongoException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoSecurityException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public MongoSecurityException(string message) 
            : base(message) 
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoSecurityException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public MongoSecurityException(string message, Exception inner) 
            : base(message, inner) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoSecurityException" /> class.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="context">The context.</param>
        protected MongoSecurityException(SerializationInfo info, StreamingContext context)
            : base(info, context) 
        {
        }
    }
}
