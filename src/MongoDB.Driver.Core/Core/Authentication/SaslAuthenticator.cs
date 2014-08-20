using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Authentication
{
    public abstract class SaslAuthenticator : IAuthenticator
    {
        // fields
        private readonly ISaslMechanism _mechanism;

        // constructors
        protected SaslAuthenticator(ISaslMechanism mechanism)
        {
            _mechanism = Ensure.IsNotNull(mechanism, "mechanism");
        }

        // properties
        public string Name
        {
            get { return _mechanism.Name; }
        }

        public abstract string DatabaseName { get; }

        // methods
        public async Task AuthenticateAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var conversation = new SaslConversation())
            {
                var currentStep = _mechanism.Initialize(connection);

                var command = new BsonDocument
                {
                    { "saslStart", 1 },
                    { "mechanism", _mechanism.Name },
                    { "payload", currentStep.BytesToSendToServer }
                };

                var slidingTimeout = new SlidingTimeout(timeout);

                while (true)
                {
                    BsonDocument result;
                    try
                    {
                        var protocol = new CommandWireProtocol(DatabaseName, command, true);
                        result = await protocol.ExecuteAsync(connection, slidingTimeout, cancellationToken);
                    }
                    catch(CommandException ex)
                    {
                        var message = string.Format("Unable to authenticate using sasl protocol mechanism {0}.", Name);
                        throw new AuthenticationException(message, ex);
                    }

                    // we might be done here if the client is not expecting a reply from the server
                    if (result.GetValue("done", false).ToBoolean() && currentStep.IsComplete)
                    {
                        break;
                    }

                    currentStep = currentStep.Transition(conversation, result["payload"].AsByteArray);

                    // we might be done here if the client had some final verification it needed to do
                    if (result.GetValue("done", false).ToBoolean() && currentStep.IsComplete)
                    {
                        break;
                    }

                    command = new BsonDocument
                    {
                        { "saslContinue", 1 },
                        { "conversationId", result["conversationId"].AsInt32 },
                        { "payload", currentStep.BytesToSendToServer }
                    };
                }
            }
        }

        // nested classes
        protected sealed class SaslConversation : IDisposable
        {
            // fields
            private List<IDisposable> _itemsNeedingDisposal;
            private bool _isDisposed;

            // constructors
            public SaslConversation()
            {
                _itemsNeedingDisposal = new List<IDisposable>();
            }

            // methods
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void RegisterItemForDisposal(IDisposable disposable)
            {
                _itemsNeedingDisposal.Add(disposable);
            }

            private void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    // disposal should happen in reverse order of registration.
                    if (disposing && _itemsNeedingDisposal != null)
                    {
                        for (int i = _itemsNeedingDisposal.Count - 1; i >= 0; i--)
                        {
                            _itemsNeedingDisposal[i].Dispose();
                        }

                        _itemsNeedingDisposal.Clear();
                        _itemsNeedingDisposal = null;
                    }

                    _isDisposed = true;
                }
            }
        }

        protected interface ISaslMechanism
        {
            // properties
            string Name { get; }

            // methods
            ISaslStep Initialize(IConnection connection);
        }

        protected interface ISaslStep
        {
            // properties
            byte[] BytesToSendToServer { get; }

            bool IsComplete { get; }

            // methods
            ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer);
        }

        protected class CompletedStep : ISaslStep
        {
            // fields
            private readonly byte[] _bytesToSendToServer;

            // constructors
            public CompletedStep()
                : this(new byte[0])
            {
            }

            public CompletedStep(byte[] bytesToSendToServer)
            {
                _bytesToSendToServer = bytesToSendToServer;
            }

            // properties
            public byte[] BytesToSendToServer
            {
                get { return _bytesToSendToServer; }
            }

            public bool IsComplete
            {
                get { return true; }
            }

            public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
            {
                throw new InvalidOperationException("Sasl conversation has completed.");
            }
        }
    }
}