using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Security
{
    /// <summary>
    /// A step indicating the expected completion of a sasl conversation. Calling Transition
    /// on this step will result in an exception indicating a communication failure between
    /// the client and server.
    /// </summary>
    internal class SaslCompletionStep : ISaslStep
    {
        // private fields
        private readonly byte[] _output;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslCompletionStep" /> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public SaslCompletionStep(byte[] output)
        {
            _output = output;
        }

        // public properties
        /// <summary>
        /// Gets the output from the transition. The output will be used to send back to the server.
        /// </summary>
        public byte[] Output
        {
            get { return _output; }
        }

        // public methods
        /// <summary>
        /// Transitions to the next step in the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="input">The input.</param>
        /// <returns>An ISaslStep.</returns>
        public ISaslStep Transition(SaslConversation conversation, byte[] input)
        {
            throw new MongoException("No more transitions available.  The conversation is completed.");
        }
    }
}