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

        public void ClusterBeforeClosing(ClusterBeforeClosingEvent @event)
        {
            _first.ClusterBeforeClosing(@event);
            _second.ClusterBeforeClosing(@event);
        }

        public void ClusterAfterClosing(ClusterAfterClosingEvent @event)
        {
            _first.ClusterAfterClosing(@event);
            _second.ClusterAfterClosing(@event);
        }

        public void ClusterBeforeOpening(ClusterBeforeOpeningEvent @event)
        {
            _first.ClusterBeforeOpening(@event);
            _second.ClusterBeforeOpening(@event);
        }

        public void ClusterAfterOpening(ClusterAfterOpeningEvent @event)
        {
            _first.ClusterAfterOpening(@event);
            _second.ClusterAfterOpening(@event);
        }

        public void ClusterBeforeAddingServer(ClusterBeforeAddingServerEvent @event)
        {
            _first.ClusterBeforeAddingServer(@event);
            _second.ClusterBeforeAddingServer(@event);
        }

        public void ClusterAfterAddingServer(ClusterAfterAddingServerEvent @event)
        {
            _first.ClusterAfterAddingServer(@event);
            _second.ClusterAfterAddingServer(@event);
        }

        public void ClusterBeforeRemovingServer(ClusterBeforeRemovingServerEvent @event)
        {
            _first.ClusterBeforeRemovingServer(@event);
            _second.ClusterBeforeRemovingServer(@event);
        }

        public void ClusterAfterRemovingServer(ClusterAfterRemovingServerEvent @event)
        {
            _first.ClusterAfterRemovingServer(@event);
            _second.ClusterAfterRemovingServer(@event);
        }

        public void ClusterAfterDescriptionChanged(ClusterAfterDescriptionChangedEvent @event)
        {
            _first.ClusterAfterDescriptionChanged(@event);
            _second.ClusterAfterDescriptionChanged(@event);
        }
    }
}