namespace IPXRelay
{
    public enum Endianness
    {
        Little,
        Big
    }

    public static class Serializer
    {
        public static unsafe byte[] Serialize<T>(T value, Endianness endianness) where T : unmanaged
        {
            byte[] buffer = new byte[sizeof(T)];

            if (endianness == Endianness.Big)
                buffer = buffer.Reverse().ToArray();

            fixed (byte* bufferPtr = buffer)
            {
                Buffer.MemoryCopy(&value, bufferPtr, sizeof(T), sizeof(T));
            }

            return buffer;
        }

        public static unsafe T Deserialize<T>(byte[] buffer, Endianness endianness) where T : unmanaged
        {
            T result = new T();

            if (endianness == Endianness.Big)
                buffer = buffer.Reverse().ToArray();

            fixed (byte* bufferPtr = buffer)
            {
                Buffer.MemoryCopy(bufferPtr, &result, sizeof(T), sizeof(T));
            }

            return result;
        }
    }
}
