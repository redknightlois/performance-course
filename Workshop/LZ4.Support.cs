using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workshop
{
    internal static class Constants
    {
        internal static class Size
        {
            public const int Kilobyte = 1024;
            public const int Megabyte = 1024 * Kilobyte;
            public const int Gigabyte = 1024 * Megabyte;
            public const long Terabyte = 1024 * (long)Gigabyte;
        }
    }

    public static class Bits
    {

        private static readonly byte[] DeBruijnBytePos64 =
            {
                0, 0, 0, 0, 0, 1, 1, 2, 0, 3, 1, 3, 1, 4, 2, 7, 0, 2, 3, 6, 1, 5, 3, 5, 1, 3, 4, 4, 2, 5, 6, 7,
                7, 0, 1, 2, 3, 3, 4, 6, 2, 6, 5, 5, 3, 4, 5, 6, 7, 1, 2, 4, 6, 4, 4, 5, 7, 2, 6, 5, 7, 6, 7, 7
            };

        private static readonly byte[] DeBruijnBytePos32 =
            {
                0, 0, 3, 0, 3, 1, 3, 0, 3, 2, 2, 1, 3, 2, 0, 1, 3, 3, 1, 2, 2, 2, 2, 0, 3, 1, 2, 0, 1, 0, 1, 1
            };


        public static int TrailingZeroes(ulong value)
        {
            return DeBruijnBytePos64[((value & (ulong)(-(long)value)) * 0x0218A392CDABBD3FUL) >> 58];
        }


        public static int TrailingZeroes(long value)
        {
            return DeBruijnBytePos64[((ulong)(value & -value) * 0x0218A392CDABBD3FUL) >> 58];
        }

        public static int TrailingZeroes(uint value)
        {
            return DeBruijnBytePos32[((value & (uint)(-(int)value)) * 0x077CB531U) >> 27];
        }


        public static int TrailingZeroes(int value)
        {
            return DeBruijnBytePos32[((uint)(value & -value) * 0x077CB531U) >> 27];
        }
    }


}
