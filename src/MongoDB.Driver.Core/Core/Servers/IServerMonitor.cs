using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Monitors a server for state changes.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal interface IServerMonitor : IDisposable
    {
        ServerDescription Description { get; }

        /// <summary>
        /// Occurs when the server description changes.
        /// </summary>
        event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Instructs the monitor to refresh its description immediately.
        /// </summary>
        void Invalidate();

        /// <summary>
        /// Requests a heartbeat as soon as possible.
        /// </summary>
        void RequestHeartbeat();
    }
}
