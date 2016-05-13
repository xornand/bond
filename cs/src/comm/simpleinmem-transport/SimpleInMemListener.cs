﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Bond.Comm.SimpleInMem
{
    using Bond.Comm.Service;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class SimpleInMemListener : Listener
    {
        private ServiceHost m_serviceHost;
        private string m_address;
        private SimpleInMemConnection m_connection;
        private readonly string m_logname;

        public SimpleInMemListener(SimpleInMemTransport parentTransport, string address)
        {
            m_address = address;
            m_serviceHost = new ServiceHost(parentTransport);
            m_connection = new SimpleInMemConnection(m_serviceHost, this, ConnectionType.Server);
            m_logname = $"{nameof(SimpleInMemListener)}({m_address})";
        }

        public override bool IsRegistered(string serviceMethodName)
        {
            return m_serviceHost.IsRegistered(serviceMethodName);
        }

        public override void AddService<T>(T service)
        {
            Log.Information("{0}.{1}: Adding {2}.", m_logname, nameof(AddService), typeof(T).Name);
            m_serviceHost.Register(service);
        }

        public override void RemoveService<T>(T service)
        {
            m_serviceHost.Deregister((IService)service);
        }

        public override Task StartAsync()
        {
            m_connection.Start();
            return TaskExt.CompletedTask;
        }

        public override Task StopAsync()
        {
            return m_connection.StopAsync();
        }

        internal void AddClient(SimpleInMemConnection client)
        {
            var connectedEventArgs = new ConnectedEventArgs(client);
            Error disconnectError = OnConnected(connectedEventArgs);

            if (disconnectError != null)
            {
                Log.Information("{0}.{1}: Rejecting connection {2} because {3}:{4}.",
                    m_logname, nameof(AddClient), client.Id, disconnectError.error_code, disconnectError.message);
                throw new SimpleInMemProtocolErrorException(
                    "Connection rejected",
                    details: disconnectError,
                    innerException: null);
            }

            m_connection.AddClientConnection(client);
        }

        internal Error InvokeOnConnected(ConnectedEventArgs args)
        {
            return OnConnected(args);
        }

        internal void InvokeOnDisconnected(DisconnectedEventArgs args)
        {
            OnDisconnected(args);
        }
    }
}
