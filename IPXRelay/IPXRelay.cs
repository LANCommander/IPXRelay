using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IPXRelayDotNet
{
    public class IPXRelay : IDisposable
    {
        private static ILogger Logger;
        public int Port { get; set; } = 213;

        private readonly Socket Socket;
        private SocketFlags Flags;

        private byte[] Buffer = new byte[65536];
        private HashSet<IPXClientConnection> Connections = new HashSet<IPXClientConnection>();

        public delegate void OnClientConnectedHandler(object sender, OnClientConnectedEventArgs e);
        public event OnClientConnectedHandler OnClientConnected;

        public delegate void OnReceivePacketErrorHandler(object sender, OnReceivePacketErrorEventArgs e);
        public event OnReceivePacketErrorHandler OnReceivePacketError;

        public delegate void OnReceivePacketHandler(object sender, OnReceivePacketEventArgs e);
        public event OnReceivePacketHandler OnReceivePacket;

        public delegate void OnSendPacketErrorHandler(object sender, OnSendPacketErrorEventArgs e);
        public event OnSendPacketErrorHandler OnSendPacketError;

        public delegate void OnSendPacketHandler(object sender, OnSendPacketEventArgs e);
        public event OnSendPacketHandler OnSendPacket;

        public IPXRelay(ILogger logger = null)
        {
            Logger = logger;

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public IPXRelay(int port, ILogger logger = null)
        {
            Port = port;
            Logger = logger;

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void DisableLogging()
        {
            Logger = null;
        }

        public IEnumerable<IPXClientConnection> GetConnections()
        {
            return Connections;
        }

        public async Task StartAsync()
        {
            Logger?.LogInformation("Binding IPX relay server on port {Port}", Port);

            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));

            var localEndPoint = new IPEndPoint(IPAddress.Any, Port);
            var remoteEndPoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);

            while (Socket.IsBound)
            {
                try
                {
                    Logger?.LogTrace("Waiting for new IPX packet");

                    var result = await Socket.ReceiveMessageFromAsync(Buffer, Flags, remoteEndPoint);

                    try
                    {
                        Logger?.LogTrace("-------- START PACKET --------");

                        localEndPoint = new IPEndPoint(result.PacketInformation.Address, Port);
                        remoteEndPoint = result.RemoteEndPoint;

                        var packet = new IPXPacket(Buffer);

                        Logger?.LogTrace("Received IPX packet | Source: {RemoteEndPoint} | Destination: {LocalEndPoint}", remoteEndPoint, localEndPoint);

                        OnReceivePacket?.Invoke(this, new OnReceivePacketEventArgs
                        {
                            RemoteEndPoint = (IPEndPoint)remoteEndPoint,
                            LocalEndPoint = localEndPoint,
                            Packet = packet
                        });

                        if (packet.Header.IsEcho())
                        {
                            if (packet.Header.IsRegistration())
                            {
                                Logger?.LogTrace("Packet received is a registration request");

                                ReserveClient((IPEndPoint)remoteEndPoint);

                                await AcknowledgeClientAsync(localEndPoint, (IPEndPoint)remoteEndPoint);

                                OnClientConnected?.Invoke(this, new OnClientConnectedEventArgs
                                {
                                    RemoteEndPoint = (IPEndPoint)remoteEndPoint,
                                    LocalEndPoint = localEndPoint,
                                    Packet = packet
                                });
                            }
                        }

                        await SendPacketAsync(packet);

                        Logger?.LogTrace("--------  END PACKET  --------");
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex, "An unknown error occurred while forwarding an incoming packet");

                        OnSendPacketError?.Invoke(this, new OnSendPacketErrorEventArgs
                        {
                            RemoteEndPoint = (IPEndPoint)remoteEndPoint,
                            LocalEndPoint = localEndPoint,
                            Data = Buffer,
                            Exception = ex
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "An unknown error occurred while processing an incoming packet");

                    OnReceivePacketError?.Invoke(this, new OnReceivePacketErrorEventArgs
                    {
                        RemoteEndPoint = (IPEndPoint)remoteEndPoint,
                        LocalEndPoint = localEndPoint,
                        Data = Buffer,
                        Exception = ex
                    });
                }
            }
        }

        public void Stop()
        {
            Socket.Close();
        }

        private async Task SendPacketAsync(IPXPacket packet)
        {
            var source = packet.Header.SourceAddress.Node.ToIPEndPoint();
            var destination = packet.Header.DestinationAddress.Node.ToIPEndPoint();

            if (packet.Header.DestinationAddress.Node.Host.Address == 0xFFFFFFFF)
            {
                Logger?.LogTrace("Broadcasting packet");

                // Broadcast
                foreach (var connection in Connections.Where(c => c.Connected))
                {
                    Logger?.LogTrace("Sending IPX packet | Source: {SourceAddress}:{SourcePort} | Destination: {DestinationAddress}:{DestinationPort}", source.Address, source.Port, connection.Endpoint.Address, connection.Endpoint.Port);

                    packet.Header.DestinationAddress.Node = new IPXNode(connection.Endpoint);

                    await Socket.SendToAsync(packet.Serialize(), connection.Endpoint);
                }
            }
            else
            {
                // Forward
                foreach (var connection in Connections.Where(c => c.Connected && c.Endpoint.Address.Equals(destination.Address) && c.Endpoint.Port == destination.Port))
                {
                    Logger?.LogTrace("Sending IPX packet | Source: {SourceAddress}:{SourcePort} | Destination: {DestinationAddress}:{DestinationPort}", source.Address, source.Port, connection.Endpoint.Address, connection.Endpoint.Port);

                    packet.Header.DestinationAddress.Node = new IPXNode(connection.Endpoint);

                    await Socket.SendToAsync(packet.Serialize(), connection.Endpoint);
                }
            }

            OnSendPacket?.Invoke(this, new OnSendPacketEventArgs
            {
                SourceEndPoint = source,
                DestinationEndPoint = destination,
                Packet = packet
            });
        }

        private void ReserveClient(IPEndPoint remoteEndPoint)
        {
            // Check if already connected
            if (Connections.Any(c => c != null && c.Endpoint == remoteEndPoint && c.Connected))
            {
                Logger?.LogInformation("Remote host requested network registration, but they are already connected");
                return;
            }

            Logger?.LogInformation("Received registration request from {RemoteEndPoint}", remoteEndPoint);

            Connections.Add(new IPXClientConnection
            {
                Connected = true,
                Endpoint = remoteEndPoint,
            });
        }

        private async Task AcknowledgeClientAsync(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            var responsePacket = new IPXPacket()
            {
                Header = new IPXPacketHeader()
                {
                    Checksum = 0xffff,
                    Length = 0x1e, // Standard IPX header size
                    TransportControl = 0x0,
                    DestinationAddress = new IPXAddress
                    {
                        Network = 0x0,
                        Node = new IPXNode(remoteEndPoint),
                        Socket = 0x2
                    },
                    SourceAddress = new IPXAddress
                    {
                        Network = 0x0,
                        Node = new IPXNode(localEndPoint),
                        Socket = 0x2
                    }
                }
            };

            Logger?.LogInformation("Acknowledge remote host {RemoteEndPoint}", remoteEndPoint);

            await Socket.SendToAsync(responsePacket.Serialize(), remoteEndPoint);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
