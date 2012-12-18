using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Security
{
    /// <summary>
    /// A high-level sasl conversation object.
    /// </summary>
    internal class SaslConversation : IDisposable
    {
        // private fields
        private bool _isDisposed;
        private List<IDisposable> _managedItemsNeedingDisposal;
        private List<IDisposable> _unmanagedItemsNeedingDisposal;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslConversation" /> class.
        /// </summary>
        public SaslConversation()
        {
            _managedItemsNeedingDisposal = new List<IDisposable>();
            _unmanagedItemsNeedingDisposal = new List<IDisposable>();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SaslConversation" /> class.
        /// </summary>
        ~SaslConversation()
        {
            Dispose(false);
        }

        // public methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initiates the specified mechanism.
        /// </summary>
        /// <param name="mechanism">The mechanism.</param>
        /// <returns>An ISaslStep.</returns>
        public ISaslStep Initiate(ISaslMechanism mechanism)
        {
            return new SaslInitiationStep(mechanism);
        }

        /// <summary>
        /// Registers a managed resource for disposal.
        /// </summary>
        /// <param name="disposable">The disposable.</param>
        public void RegisterManagedResourceForDisposal(IDisposable disposable)
        {
            _managedItemsNeedingDisposal.Add(disposable);
        }

        /// <summary>
        /// Registers an unmanaged resource for disposal.
        /// </summary>
        /// <param name="disposable">The disposable.</param>
        public void RegisterUnmanagedResourceForDisposal(IDisposable disposable)
        {
            _unmanagedItemsNeedingDisposal.Add(disposable);
        }

        // private methods
        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            // disposal should happen in reverse order of registration.
            if (disposing && _managedItemsNeedingDisposal != null)
            {
                for(int i = _managedItemsNeedingDisposal.Count - 1; i >= 0; i--)
                {
                    _managedItemsNeedingDisposal[i].Dispose();
                }

                _managedItemsNeedingDisposal.Clear();
                _managedItemsNeedingDisposal = null;
            }

            if (_unmanagedItemsNeedingDisposal != null)
            {
                for (int i = _unmanagedItemsNeedingDisposal.Count - 1; i >= 0; i--)
                {
                    _unmanagedItemsNeedingDisposal[i].Dispose();
                }

                _unmanagedItemsNeedingDisposal.Clear();
                _unmanagedItemsNeedingDisposal = null;
            }

            _isDisposed = true;
        }
    }
}