using MongoDB.Driver.Security.Gsasl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Security.Mechanisms
{
    /// <summary>
    /// A base class for implementing a mechanism using Libgsasl.
    /// </summary>
    internal abstract class AbstractGsaslMechanism : ISaslMechanism
    {
        // private fields
        private readonly string _name;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractGsaslMechanism" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        protected AbstractGsaslMechanism(string name)
        {
            _name = name;
        }

        // public properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        // public methods
        /// <summary>
        /// Transitions to the next step in the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="input">The input.</param>
        /// <returns>An ISaslStep.</returns>
        /// <exception cref="MongoSecurityException">Unable to initialize context.</exception>
        public ISaslStep Transition(SaslConversation conversation, byte[] input)
        {
            GsaslContext context;
            try
            {
                context = GsaslContext.Initialize();
                conversation.RegisterUnmanagedResourceForDisposal(context);
            }
            catch (GsaslException ex)
            {
                throw new MongoSecurityException("Unable to initialize context.", ex);
            }

            GsaslSession session;
            try
            {
                session = context.BeginSession(Name);
                conversation.RegisterUnmanagedResourceForDisposal(session);
            }
            catch (GsaslException ex)
            {
                throw new MongoSecurityException("Unable to start a session.", ex);
            }

            foreach (var property in GetProperties())
            {
                session.SetProperty(property.Key, property.Value);
            }

            return new LibgsaslAuthenticateStep(session, null)
                .Transition(conversation, input);
        }

        // protected methods
        /// <summary>
        /// Gets the properties that should be used in the specified mechanism.
        /// </summary>
        /// <returns>The properties.</returns>
        protected abstract IEnumerable<KeyValuePair<string, string>> GetProperties();

        // nested classes
        private class LibgsaslAuthenticateStep : ISaslStep
        {
            private readonly byte[] _output;
            private GsaslSession _session;

            public LibgsaslAuthenticateStep(GsaslSession session, byte[] output)
            {
                _session = session;
                _output = output;
            }

            public byte[] Output
            {
                get { return _output; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] input)
            {
                try
                {
                    var output = _session.Step(input);
                    if (_session.IsComplete)
                    {
                        return new SaslCompletionStep(output);
                    }

                    return new LibgsaslAuthenticateStep(_session, output);
                }
                catch (GsaslException ex)
                {
                    throw new MongoSecurityException("Unable to authenticate.", ex);
                }
            }
        }
    }
}