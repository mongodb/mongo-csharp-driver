namespace MongoDB.Driver.Core.Events
{
    internal interface IEvent
    {
        public EventType Type { get; }
    }
}
