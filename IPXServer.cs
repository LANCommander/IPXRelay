using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace IPXRelay
{
    public class IPXServer : IDisposable
    {
        private static ILogger Logger;
        public int Port { get; set; } = 213;

        private readonly Socket Socket;
        private SocketFlags Flags;

        private byte[] Buffer = new byte[65536];
        private HashSet<IPXClientConnection> Connections = new HashSet<IPXClientConnection>();

        public IPXServer(ILogger logger = null) {
            Logger = logger;

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public IPXServer(int port, ILogger logger = null) {
            Port = port;
            Logger = logger;

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Start()
        {
            Logger?.LogInformation("Binding IPX relay server on port {Port}", Port);

            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));

            var localEndPoint = new IPEndPoint(IPAddress.Any, Port);
            var remoteEndPoint = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
            var packetInfo = default(IPPacketInformation);

            while (true)
            {
                Logger?.LogTrace("Waiting for new IPX packet");

                Socket.ReceiveMessageFrom(Buffer, 0, Buffer.Length, ref Flags, ref remoteEndPoint, out packetInfo);

                Logger?.LogTrace("-------- START PACKET --------");

                localEndPoint = new IPEndPoint(packetInfo.Address, Port);

                var packet = new IPXPacket(Buffer);

                Logger?.LogTrace("Received IPX packet | Source: {RemoteEndPoint} | Destination: {LocalEndPoint}", remoteEndPoint, localEndPoint);

                if (packet.Header.IsEcho())
                {
                    if (packet.Header.IsRegistration())
                    {
                        Logger?.LogTrace("Packet received is a registration request");

                        ReserveClient((IPEndPoint)remoteEndPoint);
                        AcknowledgeClient(localEndPoint, (IPEndPoint)remoteEndPoint);
                    }
                }

                SendPacket(packet);

                Logger?.LogTrace("--------  END PACKET  --------");
            }
        }

        public void Stop()
        {
            Socket.Close();
        }

        private void SendPacket(IPXPacket packet)
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

                    Socket.SendTo(packet.Serialize(), connection.Endpoint);
                }
            }
            else
            {
                // Forward
                foreach (var connection in Connections.Where(c => c.Connected && c.Endpoint.Address == destination.Address && c.Endpoint.Port == destination.Port))
                {
                    Logger?.LogTrace("Sending IPX packet | Source: {SourceAddress}:{SourcePort} | Destination: {DestinationAddress}:{DestinationPort}", source.Address, source.Port, connection.Endpoint.Address, connection.Endpoint.Port);

                    packet.Header.DestinationAddress.Node = new IPXNode(connection.Endpoint);

                    Socket.SendTo(packet.Serialize(), connection.Endpoint);
                }
            }
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

        private void AcknowledgeClient(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
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

            Socket.SendTo(responsePacket.Serialize(), remoteEndPoint);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
