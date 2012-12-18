using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Security
{
    /// <summary>
    /// A step initiating the sasl conversation.
    /// </summary>
    internal class SaslInitiationStep : ISaslStep
    {
        // private fields
        private readonly ISaslMechanism _mechanism;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslInitiationStep" /> class.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        public SaslInitiationStep(ISaslMechanism mechanism)
        {
            _mechanism = mechanism;
        }

        // public properties
        /// <summary>
        /// Gets the output from the transition. The output will be used to send back to the server.
        /// </summary>
        public byte[] Output
        {
            get { return new byte[0]; }
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
            return _mechanism.Transition(conversation, input);
        }
    }
}