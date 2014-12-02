using System;
using System.Net;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    public class ClusterListenerPair : IClusterListener
    {
        // static
        public static IClusterListener Create(IClusterListener first, IClusterListener second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return new ClusterListenerPair(first, second);
        }

        // fields
        private readonly IClusterListener _first;
        private readonly IClusterListener _second;

        // constructors
        public ClusterListenerPair(IClusterListener first, IClusterListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        public void BeforeClosing(ClusterBeforeClosingEvent @event)
        {
            _first.BeforeClosing(@event);
            _second.BeforeClosing(@event);
        }

        public void AfterClosing(ClusterAfterClosingEvent @event)
        {
            _first.AfterClosing(@event);
            _second.AfterClosing(@event);
        }

        public void BeforeOpening(ClusterBeforeOpeningEvent @event)
        {
            _first.BeforeOpening(@event);
            _second.BeforeOpening(@event);
        }

        public void AfterOpening(ClusterAfterOpeningEvent @event)
        {
            _first.AfterOpening(@event);
            _second.AfterOpening(@event);
        }

        public void BeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
            _first.BeforeAddingServer(@event);
            _second.BeforeAddingServer(@event);
        }

        public void AfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
            _first.AfterAddingServer(@event);
            _second.AfterAddingServer(@event);
        }

        public void BeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
            _first.BeforeRemovingServer(@event);
            _second.BeforeRemovingServer(@event);
        }

        public void AfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
            _first.AfterRemovingServer(@event);
            _second.AfterRemovingServer(@event);
        }

        public void AfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
            _first.AfterDescriptionChanged(@event);
            _second.AfterDescriptionChanged(@event);
        }
    }
}