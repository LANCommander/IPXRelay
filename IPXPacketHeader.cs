namespace IPXRelay
{
    public class IPXPacketHeader
    {
        public ushort Checksum;
        public ushort Length;
        public byte TransportControl;
        public byte PacketType;
        public IPXAddress DestinationAddress;
        public IPXAddress SourceAddress;

        public IPXPacketHeader() { }

        public IPXPacketHeader(BinaryReader reader) {
            Checksum = reader.ReadUInt16(Endianness.Big);
            Length = reader.ReadUInt16(Endianness.Big);
            TransportControl = reader.ReadByte();
            PacketType = reader.ReadByte();
            DestinationAddress = new IPXAddress(reader);
            SourceAddress = new IPXAddress(reader);
        }

        internal bool IsBroadcast()
        {
            return DestinationAddress.Network == 0xFFFFFFFF;
        }

        internal bool IsEcho()
        {
            return DestinationAddress.Socket == 0x2;
        }

        internal bool IsRegistration()
        {
            return DestinationAddress.Node.Host == 0x0;
        }
    }
}
