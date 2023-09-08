using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IPXRelay
{
    internal static class Extensions
    {
        public static byte[] ReadBytes(this BinaryReader binaryReader, Int32 count, Endianness endianness)
        {
            if (endianness == Endianness.Little)
                return binaryReader.ReadBytes(count);

            return binaryReader.ReadBytes(count).Reverse().ToArray();
        }

        public static UInt16 ReadUInt16(this BinaryReader binaryReader, Endianness endianness)
        {
            return BitConverter.ToUInt16(binaryReader.ReadBytes(2, endianness), 0);
        }

        public static UInt32 ReadUInt32(this BinaryReader binaryReader, Endianness endianness)
        {
            return BitConverter.ToUInt32(binaryReader.ReadBytes(4, endianness), 0);
        }

        public static ushort SwapBytes(this ushort x)
        {
            return (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        public static uint SwapBytes(this uint x)
        {
            return ((x & 0x000000ff) << 24) +
                   ((x & 0x0000ff00) << 8) +
                   ((x & 0x00ff0000) >> 8) +
                   ((x & 0xff000000) >> 24);
        }

        public static ulong SwapBytes(this ulong value)
        {
            ulong uvalue = value;
            ulong swapped =
                 ((0x00000000000000FF) & (uvalue >> 56)
                 | (0x000000000000FF00) & (uvalue >> 40)
                 | (0x0000000000FF0000) & (uvalue >> 24)
                 | (0x00000000FF000000) & (uvalue >> 8)
                 | (0x000000FF00000000) & (uvalue << 8)
                 | (0x0000FF0000000000) & (uvalue << 24)
                 | (0x00FF000000000000) & (uvalue << 40)
                 | (0xFF00000000000000) & (uvalue << 56));
            return swapped;
        }

        public static String ReadNullTerminatedString(this BinaryReader binaryReader, Encoding encoding)
        {
            List<Byte> byteList = new List<Byte>();

            Byte nextByte;
            while ((nextByte = binaryReader.ReadByte()) != 0)
                byteList.Add(nextByte);

            return encoding.GetString(byteList.ToArray());
        }
    }
}
