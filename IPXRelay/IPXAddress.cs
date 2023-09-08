using System.IO;

namespace IPXRelayDotNet
{
    public class IPXAddress
    {
        public uint Network;
        public IPXNode Node;
        public ushort Socket;

        public IPXAddress() { }

        public IPXAddress(BinaryReader reader)
        {
            Network = reader.ReadUInt32(Endianness.Big);
            Node = new IPXNode(reader);
            Socket = reader.ReadUInt16(Endianness.Big);
        }
    }
}
