/* This is .NET safe implementation of Crc32 algorithm.
 * This implementation has been found fastest from some variants, based on Robert Vazan native implementations of Crc32C
 * Also, it is good for x64 and for x86, so, it seems, there is no sense to do 2 different realizations.
 *
 * This algorithm is fast fork of Crc32C algorithm
 *
 * Max Vysokikh, 2016
 */

namespace OKEGui.Utils
{
    internal class SafeProxy
    {
        private const uint Poly = 0xedb88320u;

        private static readonly uint[] Table = new uint[16 * 256];

        static SafeProxy()
        {
            for (uint i = 0; i < 256; i++)
            {
                var res = i;
                for (uint t = 0; t < 16; t++)
                {
                    for (uint k = 0; k < 8; k++) res = (res & 1) == 1 ? Poly ^ (res >> 1) : (res >> 1);
                    Table[(t * 256) + i] = res;
                }
            }
        }

        public static uint Append(uint crc, byte[] input, int offset, int length)
        {
            var crcLocal = uint.MaxValue ^ crc;

            var table = Table;
            while (length >= 16)
            {
                crcLocal =
                      table[(15 * 256) + ((crcLocal ^ input[offset + 0]) & 0xff)]
                    ^ table[(14 * 256) + (((crcLocal >> 8) ^ input[offset + 1]) & 0xff)]
                    ^ table[(13 * 256) + (((crcLocal >> 16) ^ input[offset + 2]) & 0xff)]
                    ^ table[(12 * 256) + (((crcLocal >> 24) ^ input[offset + 3]) & 0xff)]
                    ^ table[(11 * 256) + input[offset + 4]]
                    ^ table[(10 * 256) + input[offset + 5]]
                    ^ table[(9 * 256) + input[offset + 6]]
                    ^ table[(8 * 256) + input[offset + 7]]
                    ^ table[(7 * 256) + input[offset + 8]]
                    ^ table[(6 * 256) + input[offset + 9]]
                    ^ table[(5 * 256) + input[offset + 10]]
                    ^ table[(4 * 256) + input[offset + 11]]
                    ^ table[(3 * 256) + input[offset + 12]]
                    ^ table[(2 * 256) + input[offset + 13]]
                    ^ table[(1 * 256) + input[offset + 14]]
                    ^ table[(0 * 256) + input[offset + 15]];
                offset += 16;
                length -= 16;
            }

            while (--length >= 0)
                crcLocal = table[(crcLocal ^ input[offset++]) & 0xff] ^ crcLocal >> 8;
            return crcLocal ^ uint.MaxValue;
        }
    }
}