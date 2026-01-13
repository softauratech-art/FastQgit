using System;

namespace FastQ.Infrastructure.Common
{
    public static class IdMapper
    {
        private static readonly byte[] Marker = { (byte)'F', (byte)'A', (byte)'S', (byte)'T', (byte)'Q', (byte)'I', (byte)'D', (byte)'0' };

        public static Guid FromLong(long value)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            Marker.CopyTo(bytes, 8);
            return new Guid(bytes);
        }

        public static bool TryToLong(Guid id, out long value)
        {
            var bytes = id.ToByteArray();
            for (var i = 0; i < Marker.Length; i++)
            {
                if (bytes[8 + i] != Marker[i])
                {
                    value = 0;
                    return false;
                }
            }

            value = BitConverter.ToInt64(bytes, 0);
            return true;
        }
    }
}
