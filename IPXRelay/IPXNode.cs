using System.IO;
using System.Net;

namespace IPXRelayDotNet
{
    public class IPXNode
    {
        public IPAddress Host;
        public ushort Port;

        public IPXNode() { }

        public IPXNode(BinaryReader reader)
        {
            Host = new IPAddress(reader.ReadUInt32());
            Port = reader.ReadUInt16();
        }

        public IPXNode(IPEndPoint endPoint)
        {
            Host = new IPAddress(endPoint.Address.Address);
            Port = (ushort)endPoint.Port;
        }

        public IPEndPoint ToIPEndPoint()
        {
            return new IPEndPoint(Host, Port);
        }
    }
}
