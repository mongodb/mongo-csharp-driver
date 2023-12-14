namespace MongoDB.Driver.Core.Servers;

/// <summary>
///  Determine which server monitoring mode to use.
/// </summary>
public enum ServerMonitoringMode
{
    /// <summary>
    /// Use polling protocol on FaaS platforms or streaming protocol otherwise. (Default)
    /// </summary>
    Auto,
    /// <summary>
    /// Use polling protocol.
    /// </summary>
    Poll,
    /// <summary>
    /// Use streaming protocol.
    /// </summary>
    Stream
}
