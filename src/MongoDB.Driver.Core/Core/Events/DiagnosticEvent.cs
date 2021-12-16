using System;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// TBD.
    /// </summary>
    public struct DiagnosticEvent
    {
        private readonly DateTime _timestamp;

        /// <summary>
        /// TDB.
        /// </summary>
        /// <param name="message"></param>
        public DiagnosticEvent(string message)
        {
            Message = message;
            _timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// TBD.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
        }
    }
}
