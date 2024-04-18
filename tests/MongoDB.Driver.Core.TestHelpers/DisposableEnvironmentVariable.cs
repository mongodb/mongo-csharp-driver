using System;

namespace MongoDB.Driver.Core.TestHelpers;

public sealed class DisposableEnvironmentVariable : IDisposable
{
    private readonly string _initialValue;
    private readonly string _name;

    public DisposableEnvironmentVariable(string variable)
    {
        var parts = variable.Split(new [] {'='}, StringSplitOptions.RemoveEmptyEntries);
        _name = parts[0];
        var value = parts.Length > 1 ? parts[1] : "dummy";
        _initialValue = Environment.GetEnvironmentVariable(_name);
        Environment.SetEnvironmentVariable(_name, value);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(_name, _initialValue);
}
