namespace IPXRelay
{
    public class IPXPacket
    {
        public IPXPacketHeader Header;
        public byte[] Data;

        public IPXPacket() { }

        public IPXPacket(byte[] data)
        {
            Deserialize(data);
        }

        public void Deserialize(byte[] data)
        {
            using (var reader = new BinaryReader(new MemoryStream(data)))
            {
                Header = new IPXPacketHeader(reader);

                var extraBytes = Header.Length - (int)reader.BaseStream.Position;

                if (extraBytes > 0)
                    Data = reader.ReadBytes(extraBytes);
            }
        }

        public byte[] Serialize()
        {
            using (var ms  = new MemoryStream(Header.Length))
            using (var packet = new BinaryWriter(ms))
            {
                packet.Write(Header.Checksum);
                packet.Write(Header.Length.SwapBytes());
                packet.Write(Header.TransportControl);
                packet.Write(Header.PacketType);
                packet.Write(Header.DestinationAddress.Network);
                packet.Write(Header.DestinationAddress.Node.Host.SwapBytes());
                packet.Write(Header.DestinationAddress.Node.Port);
                packet.Write(Header.DestinationAddress.Socket.SwapBytes());
                packet.Write(Header.SourceAddress.Network);
                packet.Write(Header.SourceAddress.Node.Host.SwapBytes());
                packet.Write(Header.SourceAddress.Node.Port);
                packet.Write(Header.SourceAddress.Socket.SwapBytes());

                if (Data != null && Data.Length > 0)
                    packet.Write(Data);

                return ms.ToArray();
            }
        }
    }
}
