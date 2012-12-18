using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Security
{
    internal interface ISaslStep
    {
        /// <summary>
        /// Gets the output from the transition. The output will be used to send back to the server.
        /// </summary>
        byte[] Output { get; }

        /// <summary>
        /// Transitions to the next step in the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="input">The input.</param>
        /// <returns>An ISaslStep.</returns>
        ISaslStep Transition(SaslConversation conversation, byte[] input);
    }
}