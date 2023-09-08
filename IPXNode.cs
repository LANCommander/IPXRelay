using System.Net;

namespace IPXRelay
{
    public class IPXNode
    {
        public uint Host;
        public ushort Port;

        public IPXNode() { }

        public IPXNode(BinaryReader reader)
        {
            Host = reader.ReadUInt32();
            Port = reader.ReadUInt16();
        }

        public IPXNode(IPEndPoint endPoint)
        {
            Host = (uint)endPoint.Address.Address;
            Port = (ushort)endPoint.Port;
        }

        public IPEndPoint ToIPEndPoint()
        {
            return new IPEndPoint(new IPAddress(Host), Port);
        }
    }
}
